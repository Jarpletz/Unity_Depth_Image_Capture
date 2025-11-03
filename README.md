# Unity Depth Image Capture

This project captures both color and depth images from a Unity 6 URP scene for every rendered frame. This data is captured for a specified time period, in which the rendered data is stored. Afterwards, each frame rendered in this time is saved as 2 PNG images - one for color, one for depth.

The code used in this project, as well as a sample scene it has been integrated with.

## Purpose
The goal is to create a consistent dataset that can be used to study how different video encoding techniques perform when sending both color and depth data of rendered scenes together. This was built specifically with the application of VR Render Offloading in mind, but it has a variety of other potential uses.

## Quick Run

Here are instructions to run the project using the sample scene:
1. Clone this repositiory
2. Open the project in Unity. It was created in Unity 6 version 6000.2.8f1, but later versions will likely work.
3. Open the "GardenScene" Scene.
4. In the Project Directory tab of the Unity editor, go to `Settings -> PC -> PC_High_Renderer -> Output Textures Feature`
    1. Set the "Texture Size" fielhd to your desired image size. Larger images will be less performant.
5. In the Scene Hierarchy, select the `RenderCapture` object. In the `RenderCapture` component, set "Render Time Seconds". This is the amount of time for which the scene will capture each render to save as a PNG.
6. Run the project within the editor, or create a build for it, then run that.
    1. After rendering for the specified time period, "Done Capturing Renders" text will appear to notifiy you that it is done capturing renders
    2. Your File system navigation system will open to the directory the images are being saved to. This may take some time.
    3. After all images have been saved, "Done saving PNGs" text will appear in the application.
    4. You may close the application.


## Pipeline Overview

This Image capturing is was accompliehsed by using a custom URP RenderFeature to obtain RenderTextures of the color and depth data, then a separate script to asynchronously save the captured data.

The Custom Render Feature (`OutputTexturesFeature.cs`:
- Creates two RenderTextures: one containing the camera color buffer and one containing the camera depth buffer.
- Both textures are copied from the active camera using RenderGraph and URP internal resources (`cameraOpaqueTexture`, `cameraDepthTexture`).
- Each texture is blitted to GPU-managed RenderTextures.
- The textures are public static attributes of the render feature, making them publically accessible via script. 

The Script that saves the rendered textures (`RenderCapture.cs`):
- When each frame is finished rendering, submits request to asynchronously read back the texture data (above) to the CPU.
- The resulting raw byte data for both color and depth images are stored in a queue until capture ends.
- Manages a timer to determine when capturing ends. 
- After the specified capture duration ends, all queued frames are saved as PNG files to disk.
  

## Credits

This project assets created by others, as follows:
- The custom Render Feature used to obtain RenderTextures of both the Depth and Color render data was created by modifying the Unity URP OutputTextureRendererFeature Render Feature.
- The sample scene used to demonstrate the Image Capturer is the "Garden" scene from the free Unity URP Samples Project.
- All Unity-related assets and code remain subject to Unity’s original licensing terms.
- 
