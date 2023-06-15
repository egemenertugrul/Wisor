#ifndef _PIPE_RW
#define _PIPE_RW
#include <unistd.h>

int open_pipe(const char* pipeName);
int read_from_pipe(int pipe_fd, char* buffer, size_t buffer_size);
ssize_t write_to_pipe(int pipe_fd, const char* message);
void close_pipe(int pipe_fd);

#endif