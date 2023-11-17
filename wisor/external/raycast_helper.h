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

#ifndef _RAYCAST
#define _RAYCAST

#include <raylib.h>
#include <stdio.h>
#include "raymath.h"

 // a key value of 0 causes GLFW to complain!
#define KEY_INVALID 163 // something that isn't a key? but disables that function

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

#endif