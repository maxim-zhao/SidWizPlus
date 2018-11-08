# SidWizPlus
Fork of a version of SidWiz with modifications for my own purposes.

## Features added

* Commandline mode
* Replaced renderer with GDI+ (.net Graphics API), allowing simpler code and more advanced rendering:
  * Antialiasing
  * Bitmap layering
* Added rendering features from other variants
  * Grid
  * Channel labels
* Integration of [MultiDumper](https://bitbucket.org/losnoco/multidumper) to generate tracks from a VGM
  * Including automatic removal of unused tracks
* Waveform scaling
  * Including auto-scaling (e.g. scale peak to 100%)
* Automatic master audio track generation, with optional ReplayGain
* One-shot audio+video file creation via FFMPEG with optional preview
* Or run in preview-only mode
* Unlimited tracks and columns
* Render to any size video
* YouTube uploading (commandline only)
  * Including generation of titles and descriptions from tags in VGM files
* New GUI using the same renderer to give previews as you change settings
  * Almost-live updates as you edit settings
  * Preview rendering and data loading on background threads so it's pretty fast
  * Select your preview location as you go
  * Most parameters editable per-channel

![GUI](https://i.imgur.com/8qk17Md.png)
