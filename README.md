# Unity Depth Image Capture

This project captures both color and depth images from a Unity 6 URP scene for every rendered frame.
During a specified capture period, each frame’s color and depth data are stored and then saved as two separate PNG images—one for color, one for depth.

An example scene is provided, as well as previews of the two saved render images:
<img width="1919" height="883" alt="image" src="https://github.com/user-attachments/assets/15b81ce4-4189-4707-99dc-b2f237cca27e" />




The code used in this project, as well as a sample scene it has been integrated with, are provided in this repository.

## Purpose
The goal is to create a consistent dataset that can be used to study how different video encoding techniques perform when sending both color and depth data of rendered scenes together. This was built specifically with the application of VR Render Offloading in mind, but it has a variety of other potential uses.

## Quick Run

Here are instructions to run the project using the sample scene:
1. Clone this repositiory
2. Open the project in Unity. (Tested in `6000.2.8f1`)
3. Open the "GardenScene" Scene.
4. In the Project Directory tab of the Unity editor, go to `Settings -> PC -> PC_High_Renderer -> Output Textures Feature`
    1. Set the "Texture Size" fielhd to your desired image size. Larger images will be less performant.
5. In the Scene Hierarchy, select the `RenderCapture` object. In the `RenderCapture` component, set "Render Time Seconds". This is the amount of time for which the scene will capture each render to save as a PNG.
6. Run the project within the editor, or create a build for it, then run that.
    1. After rendering for the specified time period, "Done Capturing Renders" text will appear to notifiy you that it is done capturing renders
    2. Your File system navigation system will open to the directory the images are being saved to. This may take some time.
    3. After all images have been saved, "Done saving PNGs" text will appear in the application.
    4. You may close the application.
7. The Captured images will be saved in a new `Output` folder in your project/build directory.
    1. The captured images follow the following naming convention (using their frame number in their name):
        * `Color_00xxx.png`
        * `Depth_00xxx.png`

## Implementation Instructions

Instructions for implementing this in another Unity Project:
- Ensure you are on Unity `6000.xx+`
- Confugure your project for Unity Universal Render Pipeline.
- Copy the scripts and shader in this repository's root directory into your project.
- Locate the Universal Render Pipeline Asset used by your project
  - Under Rendering, make sure "Opaque Texture" and "Depth Texture" are both checked.
- Locate the Universal Render Data asset used by your project. It Should be referenced by the URP Asset in the previous step.
  - Click "Add Render Feature"
  - Add an "Output Textures Feature" Render feature.
  - Set the Texture Size to your desired image resolution.
- Go to `Edit -> Project Settings -> Graphics -> Shader Settings -> Always Includeed Shaders`
  - Add "Shader Graphs/BlitTargetTexture" To the list.
- Open your desired scene, go to its hierarchy tab
- Create a new GameObject and add a `RenderCapture` Component to it.
  - Set the "Render Time Seconds" to the amount of time you want to capture frames for
  - **Optional**: If you would like to display text when capturing and saving is complete:
    - Create a UI Object with text saying something like "Done Capturing Images". In the `RenderCapture` component, add a reference to this gameobject.
    - Create a UI Object with text saying something like "Done Saving PNGs". In the `RenderCapture` component, add a reference to this gameobject.
    - Disable both GameObjects. The `RenderCapture` script will enable them at runtime when appropriate.
- **Optional:** if you would like to display the render textures created by the custom Render Feature in your application:
  - Create two UI gameObjects, and add `RawImage` Components to each of them.
  - In the same gameobject as the `RenderCapture` component, add a `PreviewRenderTextures` component.
  - Add references to the `RawImage` components you created in the `DepthDisplay` and `ColorDisplay` fields.
- Congratulations, the scene should now be fully configured to capture and save color and depth images of each frame! 


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
- The custom Render Feature used to obtain RenderTextures of both the Depth and Color render data was created by modifying the Unity URP `OutputTextureRendererFeature` Render Feature.
  - The shader this Feature uses also is fromt he Unity URP `OutputTextureRendererFeature` Sample. 
- The sample scene used to demonstrate the Image Capturer is the "Garden" scene from the free Unity URP Samples Project.
- All Unity-related assets and code remain subject to Unity’s original licensing terms.
- 
