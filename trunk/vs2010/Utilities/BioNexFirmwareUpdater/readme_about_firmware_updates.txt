In order to embed your firmware in the updater utility, you need to following the naming convention.
Create a folder (if necessary) for your product.  The product name is whatever it is in DeviceInterface.BioNexDeviceNames.
Only put the latest firmware in the folder!  The format is {axis_id}_{major}.{minor}.sw
Ensure that all .sw files are set to "Embedded Resource" in the properties window