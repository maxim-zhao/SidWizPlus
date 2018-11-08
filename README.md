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
* Automatic master audio track generation, with ReplayGain
* Waveform scaling
  * Including auto-scaling (e.g. scale peak to 100%)
* Unlimited tracks and columns
* YouTube uploading
  * Including generation of titles and descriptions from tags in VGM files
* New GUI using the same renderer to give previews as you change settings

![Preview](https://i.imgur.com/EWO0Eq8.png)
