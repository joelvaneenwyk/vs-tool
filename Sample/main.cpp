#include <string>
#include <cstdio>

#ifndef _WIN32
#include <emscripten.h>
#endif

void printMessage(const char *message)
{
#ifndef _WIN32
    // Alternative that uses JavaScript
    char buffer[2048];
    snprintf(buffer, 2048, "console.log('Using console.log: %s');", message);
    emscripten_run_script(buffer);
#endif

    // The newline is actually necessary to force it to flush
    printf("%s\n", message);
}

int main()
{
    printMessage("Hello from emscripten world!");
    return 1;
}