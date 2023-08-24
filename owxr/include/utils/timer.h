#ifndef _UTIL_TIMER
#define _UTIL_TIMER

#include <stdbool.h>
#include "raylib.h"

typedef void (*TimerCallback)(void);

typedef struct Timer {
    double startTime;   // Start time (seconds)
    double lifeTime;    // Lifetime (seconds)
    bool isRecurring;
	TimerCallback callback;
    bool isEnded;
} Timer;

void StartTimer(Timer *timer, double lifetime, bool isRecurring, TimerCallback callback)
{
    timer->startTime = GetTime();
    timer->lifeTime = lifetime;
    timer->isRecurring = isRecurring;
	timer->callback = callback;
    timer->isEnded = false;
}

void StartTimers(Timer *timers, size_t numTimers, double lifetime, bool isRecurring, TimerCallback callback)
{
    for (size_t i = 0; i < numTimers; i++) {
        StartTimer(&timers[i], lifetime, isRecurring, callback);
    }
}

bool CheckTimerDone(Timer *timer)
{
    if(timer->isEnded)
        return false;

    bool timer_done = GetTime() - timer->startTime >= timer->lifeTime;
    if (timer_done){
        timer->isEnded = true;
        if(timer->callback != NULL)
            timer->callback();
    
        if(timer->isRecurring)
            StartTimer(timer, timer->lifeTime, timer->isRecurring, timer->callback);
    }


    return timer_done;
}

void CheckTimersDone(Timer *timers, size_t numTimers)
{
    printf("Checking timers: %d\n", (int)numTimers);
    for (size_t i = 0; i < numTimers; i++) {
        CheckTimerDone(&timers[i]);
    }
}


double GetElapsed(Timer *timer)
{
    return GetTime() - timer->startTime;
}

#endif