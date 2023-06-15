#ifndef HOVERLIB_H
#define HOVERLIB_H

#include <stdbool.h>
#include "raylib.h"

#define MAX_ANGLE 360.0f

typedef struct
{
    bool isHovered;
    float hoverDuration;
    float requiredHoverTime;

    bool callbackExecuted;
} HoverElement;

typedef struct
{
    Vector2 position;
    float fillAmount;
    Color color;
    HoverElement element;
} HoverCursor;

typedef void (*HoverCallback)(void);

void InitializeHoverElement(HoverElement *element, float requiredHoverTime);

// void UpdateHoverElement(HoverElement *element, HoverCallback callback);

#endif  // HOVERLIB_H
