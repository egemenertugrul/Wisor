#ifndef _UTIL_IMAGE_LOADER
#define _UTIL_IMAGE_LOADER

// Load image from Color array data (RGBA - 32bit)
// Creates a copy of pixels data array
// NOTE: Added here since the newer version of raylib no longer supports this feature

Image LoadImageEx(Color *pixels, int width, int height)
{
	Image image = {0};
	image.data = NULL;
	image.width = width;
	image.height = height;
	image.mipmaps = 1;
	image.format = PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;

	int k = 0;

	image.data = (unsigned char *)RL_MALLOC(image.width * image.height * 4 * sizeof(unsigned char));

	for (int i = 0; i < image.width * image.height * 4; i += 4)
	{
		((unsigned char *)image.data)[i] = pixels[k].r;
		((unsigned char *)image.data)[i + 1] = pixels[k].g;
		((unsigned char *)image.data)[i + 2] = pixels[k].b;
		((unsigned char *)image.data)[i + 3] = pixels[k].a;
		k++;
	}

	return image;
}

#endif