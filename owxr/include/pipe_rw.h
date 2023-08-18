#ifndef _PIPE_RW
#define _PIPE_RW
#include <unistd.h>
#include <stdbool.h>

#define SENSOR_ITEMS_LENGTH 3

typedef struct
{
    bool state;
	const char* name;
	const char* text;
} Status;

typedef struct IMU_Data {
	double accX, accY, accZ;
	// double gyroX, gyroY, gyroZ;
	double pitch, yaw, roll;
	double time;
} IMU_Data;

int open_pipe(const char* pipeName);
int read_from_pipe(int pipe_fd, char* buffer, size_t buffer_size);
ssize_t write_to_pipe(int pipe_fd, const char* message);
void close_pipe(int pipe_fd);

#endif