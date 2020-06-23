# Library-Bridger-2
Revision of previous Library Bridger application. Features much cleaner code and refined UI.

Features:
- Scan and search for Spotify tracks equivalent to tracks in your local music library.
- Edit found tracks in case of mismatches.
- Create playlists from found tracks.
- Identify songs that are missing tags or filenames by Audio Fingerprinting.
- Backup your search results for later editing.

Setting up:
For the application to work you need to sign up on https://developer.spotify.com/. After creating an app in your dashboard you need to copy your Client ID and paste it into the Settings window in Library Bridger 2. After this you can authorize your session and start searching for your songs.
Additionaly, if you want to use AcoustID to identify songs by their audio you need to sign up here: https://acoustid.org/new-application and create an appliaction. Like before, after this you will get an API Key that you will need to paste into the Settings window. To enable audio fingerpinting just tick the bottom checkbox and paste the key. This should be it.
