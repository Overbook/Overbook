# Overbook

Server to load audio books in [Overcast](https://overcast.fm) through RSS.

## Setup

- You need to set up a file server (preferrably with authentication). The example is `https://username:password@overbook.example.com/feed.xml`
- Build `Overbook/Overbook.sln` with Visual Studio
- Copy `Overbook/feed.xml` and `Overbook/feed.png` to `Overbook/bin/Debug/www`
- Modify `feed.xml` with your website URL
- You can run `Overbook.exe "<audiobook-folder>"`. The MP3s will be merged and metadata appended to `feed.xml`
- Copy the contents of the `www` folder to your server
- Add `https://username:password@overbook.example.com/feed.xml` to your podcast player of choice