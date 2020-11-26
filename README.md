# SPAWSH
spawsh is a command line client for gemini. It can be used as a curl-ish utility or in interactive mode.

## Usage
run  spawsh with the url requested as the first argument. Do not prepend the url with gemini://.
e.g. ./spawsh.exe gemini.circumlunar.space/docs/

spawsh will fetch the file and display in terminal.

Run spawsh with -i as the flag to enter interactive mode. Use right and left arrows to cycle through the links on the page and press enter to go to that link.
If no link is selected, press enter, then type in the new url, and enter again, and the new page will be fetched.