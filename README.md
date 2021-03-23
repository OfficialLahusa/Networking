
# C# Client-Server Networking
This project is a multiplayer shooter game developed in **.Net 5.0** C# as a networking learning project. It is based on a previous networking project written in C++, this is intended as its successor and has an expanded feature set and improved performance.  Due to its complexity, the project is split up into multiple Visual C# Projects:

 - **Client**  
   Visual client for configuring the player, connecting and interacting with the server
 - **Server**  
   Command line server
 - **StressTestCockpit**  
   Command line stress test control
 - LahusaPackets (*required library for all of the above*)  
  Selfmade packet handling library
 - MapToolkit (*required library for client and server*)  
   Selfmade SVG Map loading library
 - SFML_Engine (*required library for client)*  
   Selfmade 2D game engine based on the Simple and Fast Multimedia Library ([SFML](https://www.sfml-dev.org/)), of which I use the .NET binding. It has a game state machine, resource management, input- and event handling as well as support for overlay states.

**Note:** All executables have an integrated config loading system. The "config.yaml" file is contained in the executable working directory and will be restored when deleted.

## Installation
Download the code from GitHub and install the following dependencies:
### NuGet Dependencies:
 - [**SFML.Net**](https://www.nuget.org/packages/SFML.Net/2.5.0?_src=template) v2.5.0
 - [**Aether.Physics2D**](https://www.nuget.org/packages/Aether.Physics2D/1.5.0?_src=template) v1.5.0
 - [**YamlDotNet**](https://www.nuget.org/packages/YamlDotNet/9.1.4?_src=template) v9.1.4
 - [**System.Runtime**](https://www.nuget.org/packages/System.Runtime/4.3.1?_src=template) v4.3.1

## Building and Running
Open the .sln file in Visual Studio 2019 and build the Server and Client (minimal setup). Make sure your output directories contain all the provided runtimes and resources.
Run the server binary first, then start up the client and configure how you want your character to look and which host you want to connect to. 

© Lasse Huber-Saffer
