#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#include <unistd.h>
#include <stdbool.h>

#include "raylib.h"
#include "raymath.h"

#define RLIGHTS_IMPLEMENTATION
#include "rlights.h"

#if defined(PLATFORM_DESKTOP)
#define GLSL_VERSION        120
#else   // PLATFORM_RPI, PLATFORM_ANDROID, PLATFORM_WEB
#define GLSL_VERSION        100
#endif

#include "nukDefs.h"
#define RAYLIB_NUKLEAR_IMPLEMENTATION
#include "raylib-nuklear.h"
#include "raycast_helper.h"

#include "parson.h"

#include "pipe_rw.h"
#include "hoverlib.h"
#include "utils.h"

#define screenWidth 1280
#define screenHeight 720

#define halfWidth screenWidth / 2
#define halfHeight screenHeight / 2

#define GAZE_CIRCLE_RADIUS 25
#define UI_WIDTH 512
#define UI_HEIGHT 512

#define ARRAY_LENGTH(arr) (sizeof(arr) / sizeof(arr[0]))

int to_renderer_pipe_fd = -1;
int to_core_pipe_fd = -1;

IMU_Data lastSensorData;
char* ssidName;

#define STATUS_ITEMS_LENGTH 4
Status statusItems[STATUS_ITEMS_LENGTH];

#define MENU_ITEMS_LENGTH 4

typedef struct MenuItem
{
	const char *text;
	HoverElement element;
	HoverCallback callback;
} MenuItem;

void processReceivedMessage()
{
	char message[1024];
	ssize_t bytesRead = read_from_pipe(to_renderer_pipe_fd, message, sizeof(message) - 1);

	if (bytesRead > 0)
	{
		// Null-terminate the received message
		message[bytesRead] = '\0';

		// Process the received message
		JSON_Value *rootValue = json_parse_string(message);
		if (rootValue == NULL)
		{
			// JSON parsing failed
			printf("Error: Failed to parse JSON\n");
			return;
		}

		JSON_Object *jsonObject = json_value_get_object(rootValue);
		const char *type = json_object_dotget_string(jsonObject, "type");
		// const char* data = json_object_dotget_string(jsonObject, "data");

		printf("==RENDERER== Received message from Python:\n");
		printf("\tType: %s\n", type);
		// printf("\tData: %s\n", data);
		printf(message);

		if (strcmp(type, "Greeting") == 0)
		{
			// printf("Processing Greeting message...\n");
		}
		else if (strcmp(type, "Sensor") == 0)
		{
			// printf("Processing Sensor message...\n");
			// Perform actions specific to Update message
			// Get the 'data' object
			JSON_Object *dataObject = json_object_get_object(jsonObject, "data");

			// Get the 'acc' array
			JSON_Array *accArray = json_object_get_array(dataObject, "acc");

			// Get the 'gyro' array
			JSON_Array *rotArray = json_object_get_array(dataObject, "rot");

			// Get the 'time' value
			double timeValue = json_object_get_number(dataObject, "time");

			// Access the values of 'acc', 'gyro', and 'time'
			double accX = json_array_get_number(accArray, 0);
			double accY = json_array_get_number(accArray, 1);
			double accZ = json_array_get_number(accArray, 2);

			double pitch = json_array_get_number(rotArray, 0);
			double yaw = json_array_get_number(rotArray, 1);
			double roll = json_array_get_number(rotArray, 2);

			// Print the values
			printf("Acc: %.2f, %.2f, %.2f\n", accX, accY, accZ);
			printf("Rot: %.2f, %.2f, %.2f\n", pitch, yaw, roll);
			printf("Time: %.2f\n", timeValue);

			lastSensorData.accX = accX;
			lastSensorData.accY = accY;
			lastSensorData.accZ = accZ;

			lastSensorData.pitch = pitch;
			lastSensorData.yaw = yaw;
			lastSensorData.roll = roll;
		}
		else if (strcmp(type, "Status") == 0)
		{
			JSON_Object *dataObject = json_object_get_object(jsonObject, "data");
			bool isWiFiEnabled = json_object_get_boolean(dataObject, "wifi");
			bool isIMUEnabled = json_object_get_boolean(dataObject, "imu");
			const char* ssid = json_object_get_string(dataObject, "ssid");
			bool isCameraEnabled = json_object_get_boolean(dataObject, "camera");

			Status wifiStatus = { .name="WiFi", .state=isWiFiEnabled };
			if(ssidName)
				free(ssidName);
			ssidName = (char*)malloc(strlen(ssid) + 1);

			Status ssidStatus = { .name="SSID", .state=isWiFiEnabled, .text=strcpy(ssidName, ssid) };
			Status imuStatus = { .name="IMU", .state=isIMUEnabled };
			Status cameraStatus = { .name="Camera", .state=isCameraEnabled };
			
			statusItems[0] = wifiStatus;
			statusItems[1] = ssidStatus;
			statusItems[2] = imuStatus;
			statusItems[3] = cameraStatus;
		}

		// Cleanup JSON resources
		json_value_free(rootValue);
	}
}

void sendMessageToCore(const char *type, const char *data)
{
	JSON_Value *rootValue = json_value_init_object();
	JSON_Object *jsonObject = json_value_get_object(rootValue);
	json_object_dotset_string(jsonObject, "type", type);
	json_object_dotset_string(jsonObject, "data", data);

	char *serializedMessage = json_serialize_to_string(rootValue);
	serializedMessage[strlen(serializedMessage)] = '\0';
	ssize_t bytesWritten = write_to_pipe(to_core_pipe_fd, serializedMessage);

	if (bytesWritten > 0)
	{
		// Message sent successfully
	}
	// Cleanup JSON resources
	json_free_serialized_string(serializedMessage);
	json_value_free(rootValue);
}

float viewAlpha = 1.0f;
bool fadeIn = true;
bool fadeOut = false;

#pragma region Messages to core

void C_ConnectToWifi()
{
	sendMessageToCore("Command", "QRScan");
}

void C_Shutdown()
{
	printf("Shutdown");
}

#pragma endregion

float CalculateFillAmount(float duration, float maxDuration)
{
	if (duration >= maxDuration)
		return 1.0f;
	else
		return duration / maxDuration;
}

void DrawFilledCircle(float x, float y, float radius, float fillAmount, Color color)
{
	float angle = (float)(fillAmount * MAX_ANGLE);
	float startAngle = angle;
	float endAngle = 0;
	int minSegments = (int)ceilf((endAngle - startAngle) / 90);
	DrawRing((Vector2){x, y}, 25, 50, startAngle, endAngle, (int)minSegments, Fade(MAROON, 0.3f));
}

int main(void)
{
	// PIPE

	const char *to_renderer_pipe_name = "/tmp/to_renderer";
	const char *to_core_pipe_name = "/tmp/to_core";

	to_renderer_pipe_fd = open_pipe(to_renderer_pipe_name);
	if (to_renderer_pipe_fd == -1)
	{
		close_pipe(to_renderer_pipe_fd);
		return 1;
	}

	to_core_pipe_fd = open_pipe(to_core_pipe_name);
	if (to_core_pipe_fd == -1)
	{
		close_pipe(to_core_pipe_fd);
		return 1;
	}

	// Initialization
	//--------------------------------------------------------------------------------------
	InitWindow(screenHeight, screenWidth, "OpenWiXR");

	// VR device parameters definition
    VrDeviceInfo device = {
        .hResolution = screenWidth,                 // Horizontal resolution in pixels
        .vResolution = screenHeight,                 // Vertical resolution in pixels
        .hScreenSize = 0.133793f,            // Horizontal size in meters
        .vScreenSize = 0.0669f,              // Vertical size in meters
        .vScreenCenter = 0.04678f,           // Screen center in meters
        .eyeToScreenDistance = 0.041f,       // Distance between eye and display in meters
        .lensSeparationDistance = 0.07f,     // Lens separation distance in meters
        .interpupillaryDistance = 0.07f,     // IPD (distance between pupils) in meters

        // NOTE: CV1 uses fresnel-hybrid-asymmetric lenses with specific compute shaders
        // Following parameters are just an approximation to CV1 distortion stereo rendering
        .lensDistortionValues[0] = 1.0f,     // Lens distortion constant parameter 0
        .lensDistortionValues[1] = 0.0f,    // Lens distortion constant parameter 1
        .lensDistortionValues[2] = 0.0f,    // Lens distortion constant parameter 2
        .lensDistortionValues[3] = 0.0f,     // Lens distortion constant parameter 3
        .chromaAbCorrection[0] = 1.0f,     // Chromatic aberration correction parameter 0
        .chromaAbCorrection[1] = 0.0f,    // Chromatic aberration correction parameter 1
        .chromaAbCorrection[2] = 1.0f,     // Chromatic aberration correction parameter 2
        .chromaAbCorrection[3] = 0.0f,       // Chromatic aberration correction parameter 3
    };

	// Load VR stereo config for VR device parameteres (Oculus Rift CV1 parameters)
    VrStereoConfig config = LoadVrStereoConfig(device);

	// Distortion shader (uses device lens distortion and chroma)
    //Shader distortion = LoadShader(0, TextFormat("resources/distortion%i.fs", GLSL_VERSION));
    Shader distortion = LoadShader(0, "resources/distortion_openwixr_120.fs");
    
    const float _offset[] = { 0.0565f, 0.0f };
    const float _distortion[] = { 0.3f };
    const float _cubicDistortion[] = { 0 };
    const int _isRight[] = { 0 };
    const float _scale[] = { 1.0f };
    const float _OutOfBoundColour[] = { 0,0,0,1 };

    const float leftScreenCenter[] = { 0.25f, 0.5f };
    const float rightScreenCenter[] = { 0.75, 0.5 };


    SetShaderValue(distortion, GetShaderLocation(distortion, "leftScreenCenter"),
        leftScreenCenter, SHADER_UNIFORM_VEC2);
	SetShaderValue(distortion, GetShaderLocation(distortion, "rightScreenCenter"),
        rightScreenCenter, SHADER_UNIFORM_VEC2);

    SetShaderValue(distortion, GetShaderLocation(distortion, "_offset"),
        _offset, SHADER_UNIFORM_VEC2);
    SetShaderValue(distortion, GetShaderLocation(distortion, "_distortion"),
        _distortion, SHADER_UNIFORM_FLOAT);
    SetShaderValue(distortion, GetShaderLocation(distortion, "_cubicDistortion"),
        _cubicDistortion, SHADER_UNIFORM_FLOAT);
    SetShaderValue(distortion, GetShaderLocation(distortion, "_isRight"),
        _isRight, SHADER_UNIFORM_INT);
    SetShaderValue(distortion, GetShaderLocation(distortion, "_scale"),
        _scale, SHADER_UNIFORM_FLOAT);
    SetShaderValue(distortion, GetShaderLocation(distortion, "_OutOfBoundColour"),
        _OutOfBoundColour, SHADER_UNIFORM_VEC4);

    // Initialize framebuffer for stereo rendering
    // NOTE: Screen size should match HMD aspect ratio
    RenderTexture2D target = LoadRenderTexture(device.hResolution, device.vResolution);

    // The target's height is flipped (in the source Rectangle), due to OpenGL reasons
    Rectangle sourceRec = { 0.0f, 0.0f, (float)target.texture.width, -(float)target.texture.height };
    Rectangle destRec = { (float)GetScreenWidth(), 0.0f, (float)GetScreenHeight(), (float)GetScreenWidth() };

	// // Define the camera to look into our 3d world
	// Camera camera = {0};
	// camera.position = (Vector3){0.0f, 2.5f, -3.0f};
	// camera.up = (Vector3){0.0f, 1.0f, 0.0f};
	// camera.fovy = 45.0f;
	// camera.projection = CAMERA_PERSPECTIVE;

	// Define the camera to look into our 3d world (VR)
    Camera camera = { 0 };
    camera.position = (Vector3){0.0f, 2.5f, -3.0f};    // Camera position
	camera.target = (Vector3){ 0.0f, 2.0f, 0.0f };      // Camera looking at point
    camera.up = (Vector3){ 0.0f, 1.0f, 0.0f };          // Camera up vector
    camera.fovy = 90.0f;                                // Camera field-of-view Y
    camera.projection = CAMERA_PERSPECTIVE;             // Camera type


	HoverElement elements[MENU_ITEMS_LENGTH];

	MenuItem menuItems[] = {
		{"Connect to WiFi", {false, 0.0f, 0.0f, false}, C_ConnectToWifi},
		{"(N/A) Settings", {false, 0.0f, 0.0f, false}, NULL},
		{"(N/A) Calibration", {false, 0.0f, 0.0f, false}, NULL},
		{"(N/A) Shutdown", {false, 0.0f, 0.0f, false}, C_Shutdown}};

	int i;
	for (i = 0; i < MENU_ITEMS_LENGTH; ++i)
	{
		InitializeHoverElement(&menuItems[i].element, 2.0f);
	}

	// SetCameraMode(camera, CAMERA_FIRST_PERSON);
	// SetCameraMoveControls(KEY_W, KEY_S, KEY_D, KEY_A, KEY_INVALID, KEY_INVALID);

	Quaternion rotation = QuaternionIdentity();

	// texture and shade a model
	// UV's are mapped upside down for convienience
	Model model = LoadModel("resources/ui.glb");

	// the models texture is rendered to...
	RenderTexture2D ui_target = LoadRenderTexture(UI_HEIGHT, UI_WIDTH);
	SetTextureFilter(ui_target.texture, TEXTURE_FILTER_BILINEAR); // Texture scale filter to use

	UnloadTexture(model.materials[1].maps[MATERIAL_MAP_DIFFUSE].texture);
	model.materials[1].maps[MATERIAL_MAP_DIFFUSE].texture = ui_target.texture;

	// // lighting shader
	// Shader shader = LoadShader("resources/simpleLight.vs", "resources/simpleLight.fs");
	// shader.locs[SHADER_LOC_MATRIX_MODEL] = GetShaderLocation(shader, "matModel");
	// shader.locs[SHADER_LOC_VECTOR_VIEW] = GetShaderLocation(shader, "viewPos");

	// // ambient light level
	// int amb = GetShaderLocation(shader, "ambient");
	// SetShaderValue(shader, amb, (float[4]){0.2, 0.2, 0.2, 1.0}, SHADER_UNIFORM_VEC4);

	// // set the models shader
	// model.materials[1].shader = shader;

	// // make a light (max 4 but we're only using 1)
	// Light light = CreateLight(LIGHT_POINT, (Vector3){2, 4, 1}, Vector3Zero(), WHITE, shader);

	// as this moves over the texture it is always in the centre of
	// the screen this is because we are using camera pov
	Texture cursor = LoadTexture("resources/cursor.png");

	// the render plane needs to be on the model XY plane
	// so there is less movement distortion...
	// so its tipped back a little to make it sit back flat on the floor
	// and give the "screen" a slight angle
	model.transform = MatrixRotateX(-5 * DEG2RAD);
	// dirty positioning using the model matrix
	model.transform.m13 = 1.85f;

	// the ray for the mouse and its hit info
	Ray ray = {0};
	MyRayHitInfo mhi = {0};

	// TODO not sure if its better to share a single large render
	// texture between different 3d shapes needing different gui's
	// or different nuklear context's and render textures...
	// (probably seperate context's)
	int fontSize = 32;
	Font font = LoadFontEx("resources/DMSans-Medium.ttf", fontSize, NULL, 0);
	struct nk_context *ctx = InitNuklearEx(font, fontSize);

	const void *image;
	int w, h;

	SetTargetFPS(90);

	// int display = GetCurrentMonitor();
	// SetWindowSize(GetMonitorWidth(display), GetMonitorHeight(display));
	ToggleFullscreen();

	sendMessageToCore("Command", "Begin");

	//--------------------------------------------------------------------------------------
	// Main game loop
	while (!WindowShouldClose()) // Detect window close button or ESC key
	{
		// PIPE
		//----------------------------------------------------------------------------------
		processReceivedMessage();

		// Update
		//----------------------------------------------------------------------------------

		// because the keyboard is used to move while any text editor
		// gui element is active disable camera move
		// click away from the gui element to regain movement control
		// if (editState & NK_EDIT_ACTIVE) {
		//	SetCameraMoveControls(KEY_INVALID, KEY_INVALID, KEY_INVALID, KEY_INVALID,
		//		KEY_INVALID, KEY_INVALID);
		//}
		// else {
		//	SetCameraMoveControls(KEY_W, KEY_S, KEY_D, KEY_A, KEY_INVALID, KEY_INVALID);
		//}

		{ // Get input from sensors
			Vector3 direction;
			direction.x = cosf(lastSensorData.yaw) * cosf(lastSensorData.pitch);
			direction.y = sinf(lastSensorData.pitch);
			direction.z = sinf(lastSensorData.yaw) * cosf(lastSensorData.pitch);
			camera.target = Vector3Add(camera.position, direction);
		}
		// { // Get input from keyboard
		// 	UpdateCamera(&camera, CAMERA_FIRST_PERSON);
		// }

		// Vector3 movement = { 0 }, rotation = {.x = lastSensorData.pitch, .y = lastSensorData.yaw, .z = lastSensorData.roll};
		// UpdateCameraPro(&camera, movement, rotation, 1);

		// position the light slightly above the camera
		// light.position = camera.position;
		// light.position.y += 0.1f;

		// // update the light shader with the camera view position
		// SetShaderValue(shader, shader.locs[SHADER_LOC_VECTOR_VIEW], &camera.position.x, SHADER_UNIFORM_VEC3);
		// UpdateLightValues(shader, light);

		// the ray is always down the cameras point of view
		ray = GetMouseRay((Vector2){halfHeight, halfWidth}, camera);

		// Check ray collision against model
		// NOTE: It considers model.transform matrix!
		// but NOT position in DrawModel....
		mhi = MyGetCollisionRayModel(ray, &model);
		if (mhi.subMesh != 0)
		{
			mhi.hit = false; // we only want to hit the ui mesh
		}

		// convert barrycentre coordinate into uv coordinate based
		// on the triangles uvs
		float u = mhi.bazza.x * mhi.uv1.x +
				  mhi.bazza.y * mhi.uv2.x +
				  mhi.bazza.z * mhi.uv3.x;

		float v = mhi.bazza.x * mhi.uv1.y +
				  mhi.bazza.y * mhi.uv2.y +
				  mhi.bazza.z * mhi.uv3.y;

		// mouse position and wheel
		float mx = 0, my = 0, mz = 0;

		// inject input into the GUI
		if (mhi.hit)
		{ // if we have a hit then pump the input into the gui
			nk_input_begin(ctx);
			{
				mx = UI_WIDTH * u;
				my = UI_HEIGHT - (UI_HEIGHT * v);

				nk_input_motion(ctx, (int)mx, (int)my);

				nk_input_button(ctx, NK_BUTTON_LEFT, (int)mx, (int)my, IsMouseButtonDown(MOUSE_LEFT_BUTTON));
				nk_input_button(ctx, NK_BUTTON_MIDDLE, (int)mx, (int)my, IsMouseButtonDown(MOUSE_MIDDLE_BUTTON));
				nk_input_button(ctx, NK_BUTTON_RIGHT, (int)mx, (int)my, IsMouseButtonDown(MOUSE_RIGHT_BUTTON));
				mz += (float)GetMouseWheelMove();
				nk_input_scroll(ctx, nk_vec2(0, mz));

				// nk_input_key(ctx, NK_KEY_DEL, IsKeyPressed(KEY_DELETE));
				// nk_input_key(ctx, NK_KEY_ENTER, IsKeyPressed(KEY_ENTER));
				// nk_input_key(ctx, NK_KEY_TAB, IsKeyPressed(KEY_TAB));
				// nk_input_key(ctx, NK_KEY_BACKSPACE, IsKeyPressed(KEY_BACKSPACE));
				// nk_input_key(ctx, NK_KEY_LEFT, IsKeyPressed(KEY_LEFT));
				// nk_input_key(ctx, NK_KEY_RIGHT, IsKeyPressed(KEY_RIGHT));
				// nk_input_key(ctx, NK_KEY_UP, IsKeyPressed(KEY_UP));
				// nk_input_key(ctx, NK_KEY_DOWN, IsKeyPressed(KEY_DOWN));
				// // TODO add copy paste key combos...

				// nk_input_char(ctx, GetKeyPressed());
			}
			nk_input_end(ctx);
		}
		else
		{
			// move the cursor out of the gui render area
			// and release mouse button
			nk_input_button(ctx, NK_BUTTON_LEFT, UI_WIDTH + 1, UI_HEIGHT + 1, false);
			nk_input_motion(ctx, UI_WIDTH + 1, UI_HEIGHT + 1);
			mx = UI_WIDTH + 1;
			my = UI_HEIGHT + 1;
		}

		// update the gui, (even if we are not actually pointing at it
		// it might be in view so we need to do this...
		// could check to see if the model is in view frustrum to
		// see if we need to do this...
		if (nk_begin(ctx, "OpenWiXR Window", nk_rect(0, 0, UI_WIDTH, UI_HEIGHT),
					 NK_WINDOW_BORDER | NK_WINDOW_NO_SCROLLBAR))
		{
			/* 0.2 are a space skip on button's left and right, 0.6 - size of the button */
			static const float ratio[] = {0.2f, 0.6f, 0.2f}; /* 0.2 + 0.6 + 0.2 = 1 */
			const int ROW_HEIGHT = 30, VSPACE_SKIP = 60;

			static const float pad = UI_WIDTH * 0.001;
			static const float itemW = UI_WIDTH * 0.8;
			nk_layout_row_static(ctx, (UI_HEIGHT - (ROW_HEIGHT + VSPACE_SKIP) * 3) / 2, itemW, 1);
			nk_layout_row(ctx, NK_DYNAMIC, ROW_HEIGHT, 3, ratio);
			int i;
			for (i = 0; i < MENU_ITEMS_LENGTH; i++)
			{
				nk_spacing(ctx, 1); /* skip 0.2 left */
				UpdateHoverElement(ctx, &menuItems[i].element, menuItems[i].callback);
				nk_button_label(ctx, menuItems[i].text);
				nk_spacing(ctx, 1); /* skip 0.2 right */
			}
			
			nk_layout_row_static(ctx, (UI_HEIGHT - (ROW_HEIGHT + VSPACE_SKIP * 2) * 3) / 2, itemW, 1);
			nk_layout_row(ctx, NK_DYNAMIC, ROW_HEIGHT, 3, ratio);
			int j;
			for (j = 0; j < STATUS_ITEMS_LENGTH; j++)
			{
				nk_spacing(ctx, 1); /* skip 0.2 left */
				const char* text = NULL;
				if(statusItems[j].text && strlen(statusItems[j].text) > 0){
					text = statusItems[j].text;
				} else {
					text = statusItems[j].state ? "<Enabled>" : ">Disabled<";
				} 
				nk_label(ctx, TextFormat("%s:\t %s", statusItems[j].name, text), NK_TEXT_LEFT);
				nk_spacing(ctx, 1); /* skip 0.2 right */
			}
		}
		nk_end(ctx);

		// Draw
		//----------------------------------------------------------------------------------
		// BeginDrawing();
		// {
			// ClearBackground(WHITE);

			// render to the model's "screen" texture
			BeginTextureMode(ui_target);
			{
				ClearBackground((Color){128, 128, 255, 255});
				// draw the gui
				DrawNuklear(ctx);

				int i = 0, maxVal = -1, ind = 0;
				for (i = 0; i < MENU_ITEMS_LENGTH; ++i)
				{
					if ((&menuItems[i].element)->hoverDuration > maxVal)
					{
						maxVal = (&menuItems[i].element)->hoverDuration;
						ind = i;
					}
				}

				float fillAmount = CalculateFillAmount((&menuItems[ind].element)->hoverDuration, (&menuItems[ind].element)->requiredHoverTime);
				if (fillAmount == 0)
					DrawTexture(cursor, mx, my, WHITE);

				DrawFilledCircle(mx, my, GAZE_CIRCLE_RADIUS, fillAmount, RED);
			}
			EndTextureMode();
		EndDrawing();

		BeginTextureMode(target);
            ClearBackground(BLACK);
            BeginVrStereoMode(config);
                BeginMode3D(camera);
					DrawModel(model, (Vector3){0, 0, 0}, 1, WHITE);

                    DrawCube((Vector3){ 0.0f, 0.0f, 0.0f }, 2.0f, 2.0f, 2.0f, RED);
                    DrawCubeWires((Vector3){ 0.0f, 0.0f, 0.0f }, 2.0f, 2.0f, 2.0f, MAROON);
                    DrawGrid(40, 1.0f);

                EndMode3D();
            EndVrStereoMode();
        EndTextureMode();
        
        BeginDrawing();
            ClearBackground(BLACK);
            BeginShaderMode(distortion);
                DrawTexturePro(target.texture, sourceRec, destRec, (Vector2){ 0.0f, 0.0f }, 90.0f, BLACK);
            EndShaderMode();
            // DrawFPS(10, 10);
			
			if (viewAlpha > 0)
			{
				BeginBlendMode(BLEND_ALPHA); // Enable alpha blending
				// Draw your view here with the alpha value
				DrawRectangle(0, 0, screenHeight, screenWidth, (Color){0, 0, 0, (unsigned char)(viewAlpha * 255)});
				EndBlendMode(); // Disable alpha blending
			}
        EndDrawing();

		//----------------------------------------------------------------------------------

		// Fade-in effect
		if (viewAlpha < 1.0f && fadeOut)
		{
			viewAlpha += 0.01f; // Adjust the increment value as per your desired speed
			if (viewAlpha > 1.0f)
			{
				viewAlpha = 1.0f;
				fadeOut = false;
			} // Clamp the value to 1 after reaching maximum transparency
		}

		// Fade-out effect
		if (viewAlpha > 0.0f && fadeIn)
		{
			viewAlpha -= 0.01f; // Adjust the decrement value as per your desired speed
			if (viewAlpha < 0.0f)
			{
				viewAlpha = 0.0f;
				fadeIn = false;
			}
			// Clamp the value to 0 after reaching minimum transparency
		}
	}

	// De-Initialization
	//--------------------------------------------------------------------------------------

    UnloadVrStereoConfig(config);   // Unload stereo config
    UnloadShader(distortion);       // Unload distortion shader

	UnloadModel(model);
	UnloadTexture(cursor);
	UnloadRenderTexture(target);
	UnloadRenderTexture(ui_target);
	// UnloadShader(shader);
	UnloadNuklear(ctx);
	CloseWindow(); // Close window and OpenGL context
	//--------------------------------------------------------------------------------------

	return 0;
}