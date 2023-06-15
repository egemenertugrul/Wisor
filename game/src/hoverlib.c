#include "hoverlib.h"
#include <stdio.h>
#include <raylib.h>

void InitializeHoverElement(HoverElement *element, float requiredHoverTime)
{
    element->isHovered = false;
    element->hoverDuration = 0.0f;
    element->requiredHoverTime = requiredHoverTime;
}

void UpdateHoverElement(HoverElement *element, HoverCallback callback)
{
    struct nk_context *ctx;  // You may need to modify this depending on your implementation

    if (nk_widget_is_hovered(ctx))
    {
        if (!element->isHovered)
        {
            // Cursor enters the element
            element->isHovered = true;
            element->hoverDuration = 0.0f;
            element->callbackExecuted = false;
        }
        else
        {
            // Cursor is still hovering
            element->hoverDuration += GetFrameTime();
            
            if (element->hoverDuration >= element->requiredHoverTime && !element->callbackExecuted)
            {
                // Required hover time reached
                if (callback != NULL)
                    callback();  // Call the callback function

                element->callbackExecuted = true;
            }
        }
    }
    else
    {
        // Cursor is not hovering
        element->isHovered = false;
        element->hoverDuration = 0.0f;
        element->callbackExecuted = false;  // Reset callback execution flag
    }
}
