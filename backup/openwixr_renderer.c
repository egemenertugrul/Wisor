/*******************************************************************************************
*
*   raylib [core] example - VR Simulator (Oculus Rift CV1 parameters)
*
*   Example originally created with raylib 2.5, last time updated with raylib 4.0
*
*   Example licensed under an unmodified zlib/libpng license, which is an OSI-certified,
*   BSD-like license that allows static linking with closed source software
*
*   Copyright (c) 2017-2022 Ramon Santamaria (@raysan5)
*
********************************************************************************************/

#include "raylib.h"

#if defined(PLATFORM_DESKTOP)
#define GLSL_VERSION        330
#else   // PLATFORM_RPI, PLATFORM_ANDROID, PLATFORM_WEB
#define GLSL_VERSION        100
#endif

//------------------------------------------------------------------------------------
// Program main entry point
//------------------------------------------------------------------------------------
int main(void)
{
    // Initialization
    //--------------------------------------------------------------------------------------
    const int screenWidth = 1280;
    const int screenHeight = 720;

    // NOTE: screenWidth/screenHeight should match VR device aspect ratio
    InitWindow(screenWidth, screenHeight, "openwixr renderer");

    // VR device parameters definition
    VrDeviceInfo device = {
        .hResolution = 2560,                 // Horizontal resolution in pixels
        .vResolution = 1440,                 // Vertical resolution in pixels
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
    Shader distortion = LoadShader(0, TextFormat("resources/distortion_openwixr.fs", GLSL_VERSION));
    
    const float _offset[] = { 0.0565f, 0.529f };
    const float _distortion[] = { 0.3f };
    const float _cubicDistortion[] = { 0 };
    const int _isRight[] = { 0 };
    const float _scale[] = { 1.0f };
    const float _OutOfBoundColour[] = { 0,0,0,1 };

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
    Rectangle destRec = { 0.0f, 0.0f, (float)GetScreenWidth(), (float)GetScreenHeight() };

    // Define the camera to look into our 3d world
    Camera camera = { 0 };
    camera.position = (Vector3){ 5.0f, 2.0f, 5.0f };    // Camera position
    camera.target = (Vector3){ 0.0f, 2.0f, 0.0f };      // Camera looking at point
    camera.up = (Vector3){ 0.0f, 1.0f, 0.0f };          // Camera up vector
    camera.fovy = 60.0f;                                // Camera field-of-view Y
    camera.projection = CAMERA_PERSPECTIVE;             // Camera type

    Vector3 cubePosition = { 0.0f, 0.0f, 0.0f };

    SetCameraMode(camera, CAMERA_FIRST_PERSON);         // Set first person camera mode

    SetTargetFPS(90);                   // Set our game to run at 90 frames-per-second
    //--------------------------------------------------------------------------------------

    // Main game loop
    while (!WindowShouldClose())        // Detect window close button or ESC key
    {
        // Update
        //----------------------------------------------------------------------------------
        UpdateCamera(&camera);
        //----------------------------------------------------------------------------------

        // Draw
        //----------------------------------------------------------------------------------
        BeginTextureMode(target);
        ClearBackground(BLACK);
        BeginVrStereoMode(config);
        BeginMode3D(camera);

        DrawCube(cubePosition, 2.0f, 2.0f, 2.0f, RED);
        DrawCubeWires(cubePosition, 2.0f, 2.0f, 2.0f, MAROON);
        DrawGrid(40, 1.0f);

        EndMode3D();
        EndVrStereoMode();
        EndTextureMode();

        BeginDrawing();
        ClearBackground(BLACK);
        BeginShaderMode(distortion);
        DrawTexturePro(target.texture, sourceRec, destRec, (Vector2) { 0.0f, 0.0f }, 0.0f, WHITE);
        EndShaderMode();
        DrawFPS(10, 10);
        EndDrawing();
        //----------------------------------------------------------------------------------
    }

    // De-Initialization
    //--------------------------------------------------------------------------------------
    UnloadVrStereoConfig(config);   // Unload stereo config

    UnloadRenderTexture(target);    // Unload stereo render fbo
    UnloadShader(distortion);       // Unload distortion shader

    CloseWindow();                  // Close window and OpenGL context
    //--------------------------------------------------------------------------------------

    return 0;
}