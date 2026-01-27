# BlueSky go-live commandline tool test

This is a barebones proof of concept. But aims to be more useful than the manual method. I'll probably turn it into a plugin at some point

- The first time you run it, it will generate a default config file. Edit this and fill in your details
- **Use an app password**: Settings -> Privacy and Security -> App Passwords. Don't put your main password in here *please*!
- Title can be whatever you like. Default is e.g. "LumKitty - Twitch". This shows when someone hovers over your live icon
- Description, BSky asks for it but I don't think they use it, let me know if you actually see this anywhere. Default is your Twitch channel blurb
- No need to specify a duration, this app will continually refresh your live status, and when you hit enter to end the app it will mark you as not being live
- If you kill the app instead of ending it cleanly your live status will end in 4-5 minutes
- You can override the title by specifying a new one on the command line when you run the app, allowing for per-stream customisation
- I take no responsibility if this doesn't work and you lose a million viewers. This app is rough AF and you use it at your own risk

https://twitch.tv/LumKitty
