# Open Commissioning Assistant Plugin for OfficeLite

### Description
Connects to a KUKA.OfficeLite VM using the Y200 interface.

### Quick Getting Started
- Download the zip files from the latest release page
- Unpack the `OC.OfficeLite` zip file and place it in the directory or a subdirectory of the `OC.Assistant.exe`
- Copy the `OC.OfficeLiteServer` zip file into the OfficeLite VM, unpack and start the `OC.OfficeLiteServer.exe`
- Start the Assistant and connect or create a TwinCAT solution
- Add a new plugin instance using the `+` button 
- Select `OfficeLite`, configure parameters and press `Apply` ([see also](https://github.com/OpenCommissioning/OC_Assistant?tab=readme-ov-file#installation-1))
- Depending on the parameters, a TwinCAT GVL with PLC In- and Outputs is generated 
- The plugin starts when TwinCAT goes to Run Mode and tries to connect to the `OfficeLiteServer` running within the OfficeLite VM

### Plugin Parameters
- _AutoStart_: Automatic start and stop with TwinCAT
- _IpAddress_: IP Address of the OfficeLite VM
- _Port_: The port the `OfficeLiteServer` is listening on
- _IoSize_: Size of the KRC I/O image in bytes
- _InterpolationTime_: Axis interpolation step time in milliseconds. Usually, the Y200 interface updates the axis values every 70ms. Therefore, this parameter can be used to interpolate for a smooth axis movement in the 3D visualization. Can also be turned off with value 0.

### Requirements
To run the plugin, you need a KUKA.OfficeLite VM with a valid license.\
The Y200 interface needs to be enabled within your KRC project.
See KUKA OfficeLite documentation how to create and start a Robot using OfficeLite .

> [!NOTE]
> The plugin has been tested with KUKA.OfficeLite KRC4 and KRC5 Versions `KSS 8.5`, `KSS 8.6` and `KSS 8.7`

### Credits for third-party software components
The `OC.OfficeLiteServer` uses [Fody Costura](https://www.nuget.org/packages/Costura.Fody/) to embed its references.
