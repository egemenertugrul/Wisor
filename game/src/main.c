/*
 * Copyright (c) 2019 Chris Camacho (codifies -  http://bedroomcoders.co.uk/)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

#include <stddef.h>

#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#include <unistd.h>

#include "pipe_rw.h"
#include "raylib.h"
#include "parson.h"

#include "raymath.h"

#define RLIGHTS_IMPLEMENTATION
#include "rlights.h"

#define screenWidth 1280
#define screenHeight 720

#define halfWidth screenWidth/2
#define halfHeight screenHeight/2

#include "nukDefs.h"

#define RAYLIB_NUKLEAR_IMPLEMENTATION
#include "raylib-nuklear.h"

 // a key value of 0 causes GLFW to complain!
#define KEY_INVALID 163 // something that isn't a key? but disables that function

#include "hoverlib.h"

// I need the barrycentre and uv coords of the hit triangle to
// work out the uv coordinate of the hit
// also which submesh of the model was hit
// this would be a useful addition to raylib itself...

// Raycast hit information
typedef struct MyRayHitInfo {
	bool hit;               // Did the ray hit something?
	float distance;         // Distance to nearest hit
	Vector3 position;       // Position of nearest hit
	Vector3 normal;         // Surface normal of hit
	Vector3 bazza;          // barrycentre - addition to raylib version
	Vector2 uv1, uv2, uv3;    // uv coords of each corner of triangle - addition to raylib
	int subMesh;            // which mesh in the model - additional to raylib
} MyRayHitInfo;

MyRayHitInfo MyGetCollisionRayTriangle(Ray ray, Vector3 p1, Vector3 p2, Vector3 p3);
MyRayHitInfo MyGetCollisionRayModel(Ray ray, Model* model);

// Load image from Color array data (RGBA - 32bit)
// NOTE: Creates a copy of pixels data array
Image LoadImageEx(Color* pixels, int width, int height)
{
	Image image = { 0 };
	image.data = NULL;
	image.width = width;
	image.height = height;
	image.mipmaps = 1;
	image.format = PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;

	int k = 0;

	image.data = (unsigned char*)RL_MALLOC(image.width * image.height * 4 * sizeof(unsigned char));

	for (int i = 0; i < image.width * image.height * 4; i += 4)
	{
		((unsigned char*)image.data)[i] = pixels[k].r;
		((unsigned char*)image.data)[i + 1] = pixels[k].g;
		((unsigned char*)image.data)[i + 2] = pixels[k].b;
		((unsigned char*)image.data)[i + 3] = pixels[k].a;
		k++;
	}

	return image;
}


int to_renderer_pipe_fd = -1;
int to_core_pipe_fd = -1;

typedef struct IMU_Data {
	double accX, accY, accZ;
	// double gyroX, gyroY, gyroZ;
	double pitch, yaw, roll;
	double time;
} IMU_Data;

IMU_Data lastSensorData;

void processReceivedMessage() {
	char message[1024];
	ssize_t bytesRead = read_from_pipe(to_renderer_pipe_fd, message, sizeof(message) - 1);

	if (bytesRead > 0)
	{
		// Null-terminate the received message
		message[bytesRead] = '\0';

		// Process the received message
		JSON_Value* rootValue = json_parse_string(message);
		if (rootValue == NULL) {
			// JSON parsing failed
			printf("Error: Failed to parse JSON\n");
			return;
		}
		
		JSON_Object* jsonObject = json_value_get_object(rootValue);
		const char* type = json_object_dotget_string(jsonObject, "type");
		// const char* data = json_object_dotget_string(jsonObject, "data");

		// printf("==RENDERER== Received message from Python:\n");
		// printf("\tType: %s\n", type);
		// printf("\tData: %s\n", data);
		// printf(message);

		if (strcmp(type, "Greeting") == 0) {
			// printf("Processing Greeting message...\n");
		} else if (strcmp(type, "Sensor") == 0) {
			// printf("Processing Sensor message...\n");
			// Perform actions specific to Update message
			// Get the 'data' object
			JSON_Object* dataObject = json_object_get_object(jsonObject, "data");

			// Get the 'acc' array
			JSON_Array* accArray = json_object_get_array(dataObject, "acc");

			// Get the 'gyro' array
			JSON_Array* rotArray = json_object_get_array(dataObject, "rot");

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
			// printf("Acc: %.2f, %.2f, %.2f\n", accX, accY, accZ);
			// printf("Rot: %.2f, %.2f, %.2f\n", pitch, yaw, roll);
			// printf("Time: %.2f\n", timeValue);

			lastSensorData.accX = accX;
			lastSensorData.accY = accY;
			lastSensorData.accZ = accZ;

			lastSensorData.pitch = pitch;
			lastSensorData.yaw = yaw;
			lastSensorData.roll = roll;
		}
		// Cleanup JSON resources
		json_value_free(rootValue);
	}
}

void sendMessageToCore(const char* type, const char* data) {
    JSON_Value* rootValue = json_value_init_object();
    JSON_Object* jsonObject = json_value_get_object(rootValue);
    json_object_dotset_string(jsonObject, "type", type);
    json_object_dotset_string(jsonObject, "data", data);

    char* serializedMessage = json_serialize_to_string(rootValue);
	serializedMessage[strlen(serializedMessage)] = '\0';
    ssize_t bytesWritten = write_to_pipe(to_core_pipe_fd, serializedMessage);

    if (bytesWritten > 0) {
        // Message sent successfully
    }
    // Cleanup JSON resources
    json_free_serialized_string(serializedMessage);
    json_value_free(rootValue);
}

float viewAlpha = 1.0f;
bool fadeIn = true;
bool fadeOut = false;

void MyHoverCallback()
{
    fadeOut = true;
	// printf("HoverCallback");
}

#define RADIUS 25

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
	int minSegments = (int)ceilf((endAngle - startAngle)/90);
	DrawRing((Vector2) { x, y }, 25, 50, startAngle, endAngle, (int)minSegments, Fade(MAROON, 0.3f));
}

int main(void)
{
	// PIPE

    const char* to_renderer_pipe_name = "/tmp/to_renderer";
    const char* to_core_pipe_name = "/tmp/to_core";

    to_renderer_pipe_fd = open_pipe(to_renderer_pipe_name);
    if (to_renderer_pipe_fd == -1) {
        close_pipe(to_renderer_pipe_fd);
        return 1;
    }

    to_core_pipe_fd = open_pipe(to_core_pipe_name);
    if (to_core_pipe_fd == -1) {
        close_pipe(to_core_pipe_fd);
        return 1;
    }

	// Initialization
	//--------------------------------------------------------------------------------------
	InitWindow(screenWidth, screenHeight, "raylib - test");

	// Create hover elements
    HoverElement element1;
    HoverElement element2;

    // Initialize hover elements with required hover times
    InitializeHoverElement(&element1, 2.0f);
    InitializeHoverElement(&element2, 1.5f);

	// Define the camera to look into our 3d world
	Camera camera = { 0 };
	camera.position = (Vector3){ 0.0f, 2.5f, -3.0f };
	camera.up = (Vector3){ 0.0f, 1.0f, 0.0f };
	camera.fovy = 45.0f;
	camera.projection = CAMERA_PERSPECTIVE;

	//SetCameraMode(camera, CAMERA_FIRST_PERSON);
	//SetCameraMoveControls(KEY_W, KEY_S, KEY_D, KEY_A, KEY_INVALID, KEY_INVALID);


	// texture and shade a model
	// UV's are mapped upside down for convienience
	Model model = LoadModel("resources/cube.glb");

	// the models texture is rendered to...
	RenderTexture2D target = LoadRenderTexture(512, 512);
	SetTextureFilter(target.texture, TEXTURE_FILTER_BILINEAR);  // Texture scale filter to use

	UnloadTexture(model.materials[1].maps[MATERIAL_MAP_DIFFUSE].texture);
	model.materials[1].maps[MATERIAL_MAP_DIFFUSE].texture = target.texture;

	// lighting shader
	Shader shader = LoadShader("resources/simpleLight.vs", "resources/simpleLight.fs");
	shader.locs[SHADER_LOC_MATRIX_MODEL] = GetShaderLocation(shader, "matModel");
	shader.locs[SHADER_LOC_VECTOR_VIEW] = GetShaderLocation(shader, "viewPos");

	// ambient light level
	int amb = GetShaderLocation(shader, "ambient");
	SetShaderValue(shader, amb, (float[4]) { 0.2, 0.2, 0.2, 1.0 }, SHADER_UNIFORM_VEC4);

	// set the models shader
	model.materials[1].shader = shader;

	// make a light (max 4 but we're only using 1)
	Light light = CreateLight(LIGHT_POINT, (Vector3) { 2, 4, 1 }, Vector3Zero(), WHITE, shader);

	// as this moves over the texture it is always in the centre of
	// the screen this is because we are using camera pov
	Texture cursor = LoadTexture("resources/cursor.png");


	// frame counter
	int frame = 0;

	// the render plane needs to be on the model XY plane
	// so there is less movement distortion...
	// so its tipped back a little to make it sit back flat on the floor
	// and give the "screen" a slight angle
	model.transform = MatrixRotateX(-5 * DEG2RAD);
	// dirty positioning using the model matrix
	model.transform.m13 = 1.85f;

	// the ray for the mouse and its hit info
	Ray ray = { 0 };
	MyRayHitInfo mhi = { 0 };

	// TODO not sure if its better to share a single large render
	// texture between different 3d shapes needing different gui's
	// or different nuklear context's and render textures...
	// (probably seperate context's)
	int fontSize = 13;
	Font font = LoadFontEx("resources/anonymous_pro_bold.ttf", fontSize, NULL, 0);
	struct nk_context* ctx = InitNuklearEx(font, fontSize);

	const void* image;
	int w, h;

	// variables effected by the GUI
	// track if the editor is in use
	nk_flags editState = 0;

	// option radio button
	static int op = 1;
	// slider
	static float value = 0.6f;

	// number of button clicks
	int button = 0;

	// text edited by the editor
	char name[41];
	int nameLen = 0;

	// list of options in the dropdown
	static const char* items[] = { "Apple","Bananna","Cherry","Date","Elderberry","Fig",
		"Grape","Hawthorn","Ita Palm","Jackfruit","Kiwi","Lime","Mango","Nectarine",
		"Olive","Pear","Quince","Raspberry","Strawberry","Tangerine","Ugli fruit",
		"Vanilla","Watermelon","Xylocarp","Yumberry","Zucchini" };
	static int selectedItem = 0;


	SetTargetFPS(60);               // Set  to run at 60 frames-per-second

	//--------------------------------------------------------------------------------------
	// Main game loop
	while (!WindowShouldClose())    // Detect window close button or ESC key
	{
		// PIPE

		{
			// Read the message from the pipe
			{
				processReceivedMessage();
			}
		}

		// Update
		//----------------------------------------------------------------------------------

		frame++;

		// because the keyboard is used to move while any text editor
		// gui element is active disable camera move
		// click away from the gui element to regain movement control
		//if (editState & NK_EDIT_ACTIVE) {
		//	SetCameraMoveControls(KEY_INVALID, KEY_INVALID, KEY_INVALID, KEY_INVALID,
		//		KEY_INVALID, KEY_INVALID);
		//}
		//else {
		//	SetCameraMoveControls(KEY_W, KEY_S, KEY_D, KEY_A, KEY_INVALID, KEY_INVALID);
		//}
		
		// Vector3 direction;
		// direction.x = cosf(lastSensorData.yaw) * cosf(lastSensorData.pitch);
		// direction.y = sinf(lastSensorData.pitch);
		// direction.z = sinf(lastSensorData.yaw) * cosf(lastSensorData.pitch);
		// camera.target = Vector3Add(camera.position, direction);
		UpdateCamera(&camera, CAMERA_FIRST_PERSON);
		
		// Vector3 movement = { 0 }, rotation = {.x = lastSensorData.pitch, .y = lastSensorData.yaw, .z = lastSensorData.roll};
		// UpdateCameraPro(&camera, movement, rotation, 1);

		// position the light slightly above the camera
		light.position = camera.position;
		light.position.y += 0.1f;

		// update the light shader with the camera view position
		SetShaderValue(shader, shader.locs[SHADER_LOC_VECTOR_VIEW], &camera.position.x, SHADER_UNIFORM_VEC3);
		UpdateLightValues(shader, light);

		// the ray is always down the cameras point of view
		ray = GetMouseRay((Vector2) { halfWidth, halfHeight }, camera);

		// Check ray collision against model
		// NOTE: It considers model.transform matrix!
		// but NOT position in DrawModel....
		mhi = MyGetCollisionRayModel(ray, &model);
		if (mhi.subMesh != 0) {
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
		if (mhi.hit) { // if we have a hit then pump the input into the gui
			nk_input_begin(ctx);
			{
				mx = 512 * u;
				my = 512 - (512 * v);

				nk_input_motion(ctx, (int)mx, (int)my);

				nk_input_button(ctx, NK_BUTTON_LEFT, (int)mx, (int)my, IsMouseButtonDown(MOUSE_LEFT_BUTTON));
				nk_input_button(ctx, NK_BUTTON_MIDDLE, (int)mx, (int)my, IsMouseButtonDown(MOUSE_MIDDLE_BUTTON));
				nk_input_button(ctx, NK_BUTTON_RIGHT, (int)mx, (int)my, IsMouseButtonDown(MOUSE_RIGHT_BUTTON));
				mz += (float)GetMouseWheelMove();
				nk_input_scroll(ctx, nk_vec2(0, mz));

				nk_input_key(ctx, NK_KEY_DEL, IsKeyPressed(KEY_DELETE));
				nk_input_key(ctx, NK_KEY_ENTER, IsKeyPressed(KEY_ENTER));
				nk_input_key(ctx, NK_KEY_TAB, IsKeyPressed(KEY_TAB));
				nk_input_key(ctx, NK_KEY_BACKSPACE, IsKeyPressed(KEY_BACKSPACE));
				nk_input_key(ctx, NK_KEY_LEFT, IsKeyPressed(KEY_LEFT));
				nk_input_key(ctx, NK_KEY_RIGHT, IsKeyPressed(KEY_RIGHT));
				nk_input_key(ctx, NK_KEY_UP, IsKeyPressed(KEY_UP));
				nk_input_key(ctx, NK_KEY_DOWN, IsKeyPressed(KEY_DOWN));
				// TODO add copy paste key combos...

				nk_input_char(ctx, GetKeyPressed());
			}
			nk_input_end(ctx);
		}
		else {
			// move the cursor out of the gui render area
			// and release mouse button
			nk_input_button(ctx, NK_BUTTON_LEFT, 513, 513, false);
			nk_input_motion(ctx, 513, 513);
			mx = 513;
			my = 513;
		}


		// update the gui, (even if we are not actually pointing at it
		// it might be in view so we need to do this...
		// could check to see if the model is in view frustrum to
		// see if we need to do this...
		if (nk_begin(ctx, "a window", nk_rect(10, 10, 440, 440),
			NK_WINDOW_BORDER | NK_WINDOW_MOVABLE | NK_WINDOW_CLOSABLE))
		{
			// see the fine nuklear manual....
			nk_layout_row_static(ctx, 20, 80, 1);

			if (nk_button_label(ctx, "button")) {
				button++;
				fadeOut = true;
			}
			
        	UpdateHoverElement(&element1, MyHoverCallback);

			nk_layout_row_static(ctx, 20, 80, 2);
			if (nk_option_label(ctx, "easy", op == 1)) op = 1;
			if (nk_option_label(ctx, "hard", op == 2)) op = 2;

			nk_layout_row_static(ctx, 20, 120, 1);
			nk_label(ctx, "Volume:", NK_TEXT_LEFT);
			nk_slider_float(ctx, 0, &value, 1.0f, 0.01f);

			nk_layout_row_dynamic(ctx, 20, 2);
			if (nk_combo_begin_label(ctx, items[selectedItem], nk_vec2(nk_widget_width(ctx), 200))) {
				nk_layout_row_dynamic(ctx, 10, 1);
				for (int i = 0; i < 26; ++i) {
					if (nk_combo_item_label(ctx, items[i], NK_TEXT_LEFT)) {
						selectedItem = i;
					}
				}
				nk_combo_end(ctx);
			}

			nk_layout_row_static(ctx, 24, 80, 2);
			nk_label(ctx, "Text:", NK_TEXT_LEFT);
			editState = nk_edit_string(ctx, NK_EDIT_FIELD, name, &nameLen, 40, nk_filter_ascii);

		}
		nk_end(ctx);

		// Draw
		//----------------------------------------------------------------------------------
		BeginDrawing();
		{
			ClearBackground(BLACK);

			// render to the model's "screen" texture
			BeginTextureMode(target);
			{
				ClearBackground((Color) { 128, 128, 255, 255 });
				// draw the gui
				//device_draw(&dev, ctx, 512, 512, nk_vec2(1,1), NK_ANTI_ALIASING_ON);
				DrawNuklear(ctx);
				// put a mouse cursor on the texture
				DrawTexture(cursor, mx, my, WHITE);

	            float fillAmount = CalculateFillAmount(element1.hoverDuration, element1.requiredHoverTime);
				// printf("%f", element1.hoverDuration);
				DrawFilledCircle(mx, my, RADIUS, fillAmount, RED);
			}
			EndTextureMode();

			BeginMode3D(camera);
			{
				// finally render the model
				DrawModel(model, (Vector3) { 0, 0, 0 }, 1, WHITE);
				DrawGrid(10, 1.0f);        // Draw a grid
			}
			EndMode3D();

			// just out of curiosity...
			//DrawTexture(txImg,0,0,WHITE);
			//DrawTextureEx(target.texture, (Vector2) { 0, 0 }, 0, 1, (Color) { 255, 255, 255, 128 });

			DrawFPS(10, 10);

			// show the values that are being changed by the GUI
			name[nameLen] = 0;
			DrawText(TextFormat("button presses %i  value %1.2f  op %i  Frame %i",
				button, value, op, frame), 40, 600, 48, WHITE);
			DrawText(TextFormat("ed state: %i  fruit: %s  name: %s",
				editState, items[selectedItem], name),
				40, 550, 48, WHITE);

		}
	
		if (viewAlpha > 0){
			BeginBlendMode(BLEND_ALPHA); // Enable alpha blending
			// Draw your view here with the alpha value
			DrawRectangle(0, 0, screenWidth, screenHeight, (Color){0, 0, 0, (unsigned char)(viewAlpha * 255)});
			EndBlendMode(); // Disable alpha blending
		}

		EndDrawing();
		//----------------------------------------------------------------------------------

		// Fade-in effect
		if (viewAlpha < 1.0f && fadeOut) {
			viewAlpha += 0.01f; // Adjust the increment value as per your desired speed
			if (viewAlpha > 1.0f) { 
				viewAlpha = 1.0f; 
				fadeOut = false;
			}// Clamp the value to 1 after reaching maximum transparency
		}

		// Fade-out effect
		if (viewAlpha > 0.0f && fadeIn) {
			viewAlpha -= 0.01f; // Adjust the decrement value as per your desired speed
			if (viewAlpha < 0.0f) {
				 viewAlpha = 0.0f; 
				 fadeIn = false;
			}
			// Clamp the value to 0 after reaching minimum transparency
		}
	}

	// De-Initialization
	//--------------------------------------------------------------------------------------

	UnloadModel(model);
	UnloadTexture(cursor);
	UnloadRenderTexture(target);
	UnloadShader(shader);
	UnloadNuklear(ctx);
	CloseWindow();        // Close window and OpenGL context
	//--------------------------------------------------------------------------------------

	return 0;
}

/*
 * modified ray/triangle and ray/model to additionally report back
 * barycentre and uv coords of selected triangle and sub mesh
 * (original source raylib!)
 */

 // Get collision info between ray and triangle
 // NOTE: Based on https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
MyRayHitInfo MyGetCollisionRayTriangle(Ray ray, Vector3 p1, Vector3 p2, Vector3 p3)
{
#define EPSILON 0.000001        // A small number

	Vector3 edge1, edge2;
	Vector3 p, q, tv;
	float det, invDet, u, v, t;
	MyRayHitInfo result = { 0 };

	// Find vectors for two edges sharing V1
	edge1 = Vector3Subtract(p2, p1);
	edge2 = Vector3Subtract(p3, p1);

	// Begin calculating determinant - also used to calculate u parameter
	p = Vector3CrossProduct(ray.direction, edge2);

	// If determinant is near zero, ray lies in plane of triangle or ray is parallel to plane of triangle
	det = Vector3DotProduct(edge1, p);

	// Avoid culling!
	if ((det > -EPSILON) && (det < EPSILON)) return result;

	invDet = 1.0f / det;

	// Calculate distance from V1 to ray origin
	tv = Vector3Subtract(ray.position, p1);

	// Calculate u parameter and test bound
	u = Vector3DotProduct(tv, p) * invDet;

	// The intersection lies outside of the triangle
	if ((u < 0.0f) || (u > 1.0f)) return result;

	// Prepare to test v parameter
	q = Vector3CrossProduct(tv, edge1);

	// Calculate V parameter and test bound
	v = Vector3DotProduct(ray.direction, q) * invDet;

	// The intersection lies outside of the triangle
	if ((v < 0.0f) || ((u + v) > 1.0f)) return result;

	t = Vector3DotProduct(edge2, q) * invDet;

	if (t > EPSILON)
	{
		// Ray hit, get hit point and normal
		result.hit = true;
		result.distance = t;
		result.hit = true;
		result.normal = Vector3Normalize(Vector3CrossProduct(edge1, edge2));
		result.position = Vector3Add(ray.position, Vector3Scale(ray.direction, t));
	}

	// additional barycentre info
	result.bazza = Vector3Barycenter(result.position, p1, p2, p3);

	return result;
}

// Get collision info between ray and model
MyRayHitInfo MyGetCollisionRayModel(Ray ray, Model* model)
{
	MyRayHitInfo result = { 0 };

	for (int m = 0; m < model->meshCount; m++)
	{
		// Check if meshhas vertex data on CPU for testing
		if (model->meshes[m].vertices != NULL)
		{
			// model->mesh.triangleCount may not be set, vertexCount is more reliable
			int triangleCount = model->meshes[m].vertexCount / 3;

			// Test against all triangles in mesh
			for (int i = 0; i < triangleCount; i++)
			{
				Vector3 a, b, c;
				Vector2 u1, u2, u3;
				Vector3* vertdata = (Vector3*)model->meshes[m].vertices;
				Vector2* uvdata = (Vector2*)model->meshes[m].texcoords;

				if (model->meshes[m].indices)
				{
					a = vertdata[model->meshes[m].indices[i * 3 + 0]];
					b = vertdata[model->meshes[m].indices[i * 3 + 1]];
					c = vertdata[model->meshes[m].indices[i * 3 + 2]];

					// additional uv data
					u1 = uvdata[model->meshes[m].indices[i * 3 + 0]];
					u2 = uvdata[model->meshes[m].indices[i * 3 + 1]];
					u3 = uvdata[model->meshes[m].indices[i * 3 + 2]];
				}
				else
				{
					a = vertdata[i * 3 + 0];
					b = vertdata[i * 3 + 1];
					c = vertdata[i * 3 + 2];

					// additional uv data
					u1 = uvdata[i * 3 + 0];
					u2 = uvdata[i * 3 + 1];
					u3 = uvdata[i * 3 + 2];
				}

				a = Vector3Transform(a, model->transform);
				b = Vector3Transform(b, model->transform);
				c = Vector3Transform(c, model->transform);

				MyRayHitInfo triHitInfo = MyGetCollisionRayTriangle(ray, a, b, c);
				triHitInfo.uv1 = u1;
				triHitInfo.uv2 = u2;
				triHitInfo.uv3 = u3;

				// there could be multiple hits so we
				// need to test all the tri's and find the closest
				if (triHitInfo.hit)
				{
					// Save the closest hit triangle
					if ((!result.hit) || (result.distance > triHitInfo.distance)) {
						result = triHitInfo;
						result.subMesh = m;
					}

				}
			}
		}
	}

	return result;
}

