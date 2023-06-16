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

#include "raycast_helper.h"

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

