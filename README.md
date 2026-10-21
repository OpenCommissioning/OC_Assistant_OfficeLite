# Open Commissioning Assistant Plugin for OfficeLite

### Description
Connects to a KUKA.OfficeLite VM using the Y200 interface.
The plugin communicates with the Y200 Remote Server running on the VM,
which gives access to the Y200 shared memory process image of the robot controller.

### Quick Getting Started
- Download the zip file from the latest release page
- Place the unpacked `OC.OfficeLite` folder in the `Plugins` folder of the `OC.Assistant.exe`
- Start the `Y200_Svr.exe` on the OfficeLite VM
- Make sure the VM accepts connections on the server port (default `40000`) and the IP address of the Assistant PC
is allowed in the server configuration
- Start the Assistant and open or create a project
- Add a new plugin instance using the `+` button 
- Select `OfficeLite`, configure parameters and press `Apply` ([see also](https://github.com/OpenCommissioning/OC_Assistant?tab=readme-ov-file#installation-1))
- After starting the plugin it tries to connect to the `Y200_Svr.exe`

### Plugin Parameters
- _AutoStart_: Automatic start and stop with the Assistant
- _IpAddress_: IP Address of the OfficeLite VM
- _Port_: The port the `Y200_Svr.exe` is listening on. Default is `40000`
- _Y200IpcName_: Name of the Y200 process image, see `Y200IpcName` in `C:\KRC\User\Y200Interface.config`
- _Y200WriteOffset_: Offset of the data written by the robot controller (outputs and axis values), 
see `Y200WriteOffset` in `C:\KRC\User\Y200Interface.config`
- _Y200ReadOffset_: Offset of the data read by the robot controller (inputs), 
see `Y200ReadOffset` in `C:\KRC\User\Y200Interface.config`
- _InterpolationTime_: Axis interpolation step time in milliseconds. 
The Y200 interface updates the axis values at a specific rate, 
which may depend on KSS versions or different environment settings. 
In our test cases, the update rate has been approximately 70ms. 
This parameter can be used to interpolate for a smooth axis movement in the 3D visualization. To disable interpolation, 
set the value to 0.

### Requirements
To run the plugin, you need a KUKA.OfficeLite VM with a valid license.\
The Y200 interface needs to be enabled within your KRC project.
See KUKA OfficeLite documentation how to create and start a Robot using OfficeLite .

> [!NOTE]
> The plugin has been tested with KUKA.OfficeLite KRC4 and KRC5 Versions `KSS 8.5`, `KSS 8.6` and `KSS 8.7`
