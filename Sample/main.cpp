#include <emscripten.h>
#include <string>
#include <cstdio>

void printMessage(const char *message)
{
    // Alternative that uses JavaScript
    char buffer[2048];
    snprintf(buffer, 2048, "console.log('Using console.log: %s');", message);
    emscripten_run_script(buffer);

    // The newline is actually necessary to force it to flush
    printf("%s\n", message);
}

int main()
{
    printMessage("Hello from emscripten world!");
	return 1;
}