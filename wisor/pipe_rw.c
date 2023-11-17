#include "pipe_rw.h"

#include <stdio.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/select.h>
#include <string.h>

int open_pipe(const char* pipeName) {
    int pipe_fd = open(pipeName, O_RDWR | O_NONBLOCK);
    if (pipe_fd == -1) {
        fprintf(stderr, "Failed to open the read pipe: %s\n", pipeName);
        return -1;
    }
    return pipe_fd;
}

int read_from_pipe(int pipe_fd, char* buffer, size_t buffer_size) {
    fd_set read_fds;
    FD_ZERO(&read_fds);
    FD_SET(pipe_fd, &read_fds);

    struct timeval timeout;
    timeout.tv_sec = 0;
    timeout.tv_usec = 0;

    int ready = select(pipe_fd + 1, &read_fds, NULL, NULL, &timeout);
    if (ready > 0 && FD_ISSET(pipe_fd, &read_fds)) {
        ssize_t bytes_read = read(pipe_fd, buffer, buffer_size - 1);
        if (bytes_read > 0) {
            buffer[bytes_read] = '\0';
            return bytes_read;
        }
    }
    return 0;
}

ssize_t write_to_pipe(int pipe_fd, const char* message) {
    size_t message_length = strlen(message);

    fd_set write_fds;
    FD_ZERO(&write_fds);
    FD_SET(pipe_fd, &write_fds);

    struct timeval timeout;
    timeout.tv_sec = 0;
    timeout.tv_usec = 0;

    int ready = select(pipe_fd + 1, NULL, &write_fds, NULL, &timeout);
    if (ready > 0 && FD_ISSET(pipe_fd, &write_fds)) {
        ssize_t bytes_written = write(pipe_fd, message, message_length);
        return bytes_written;
    }
    return 0;
}

void close_pipe(int pipe_fd) {
    close(pipe_fd);
}
