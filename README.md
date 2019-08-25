DokoDemoPainter
===============

DokoDemoPainter is a set of scripts and shaders for Unity3D that allows easy to
setup texture painting on regular meshes and skinned meshes. It provides a pen,
an eraser and a stamp, as well as functionality to fade texture changes.
Modified textures can be saved and saved textures can be automatically loaded
to make modifications seem persistent.

## Getting started

The easiest way to get started with DokoDemoPainter is to try out the included
example scenes. The second easiest way is to add the DokoDemoPainterPaintable
component to a textured object and add the included pen, eraser and stamp
prefabs to the scene.

Most interesting settings are on the DokoDemoPainterPen and
DokoDemoPainterStamp components. The settings on the DokoDemoPainterPaintable
component allow further fine-tuning and the use of fading and texture saving.
All fields have tooltips containing an explanation on what they do.

To make your own brush or stamp, add the corresponding component to a game
object. It is also necessary to add a camera and set it in the component.
The included prefabs can be used as a template for this.

## Demo

You can try out a very simple live demo
[here](https://emilianavt.github.io/DokoDemoPainterDemo/). A demo using WebVR
can be found [here](https://emilianavt.github.io/DokoDemoPainterVRDemo/). The
WebVR demo is based on Mozilla's Sunny Desert demo.

## Performance

While active, every pen or stamp will render to a 1x1 texture every frame to
detect painting targets. This means that every active pen or stamp in the scene
will take some processing time.

Painting itself is done using a fragment shader, so no transfers of texture
data between the CPU and GPU are necessary, so DokoDemoPainter performs
reasonably well.

Using pens with alpha blending (e.g. soft pen tips or opacity below 1.0) will
lead to a slight reduction of performance, because a second drawing history
texture will have to be updated each frame. If performance is an issue,
avoiding these functions may help.

Painting on objects with a high number of materials can also lead to reduced
performance. If the number of materials cannot be reduced, it is possible to
use the whitelist functionality of the DokoDemoPainterPaintable component to
exclude unnecessary materials from the painting process.

## Limitations

The following types of objects may cause issues with DokoDemoPainter:

  * Objects without textures. Materials have to have a set main texture,
    otherwise they cannot be painted.
  * Objects with little space between disconnected parts of their UV maps.
  * Objects where parts of the UV maps differ in scale.
  * Objects with mirrored UV maps.
  * Objects that reuse parts of their texture multiple times.
  * Objects with weird UV maps.

Using the whitelist functionality, it is possible to assign different settings
to different materials of an object by adding multiple DokoDemoPainterPaintable
components.

Issues due to difference in scale can be reduced by adjusting the radius and
size factors of the DokoDemoPainterPaintable component.

To avoid painting on unrelated parts of the texture when UV maps have little
safety distance, brush sizes should be kept small. Reducing the maxDistance
setting may also help.

If painting just won't work properly, the only way to fix it might be to adjust
the UV map of the mesh itself.

## "Painting doesn't work in a build?"

If you want to build your Unity application, you have to make sure that Unity
includes the following two shaders in the build:

  * DokoDemoPainter/Detect
  * DokoDemoPainter/Render

In the newest version, they have been moved to a "Resources" subfolder, which
seems to take care of this issue. If not, try setting the player setting
`Keep Loaded Shaders Alive` and add the two shaders to the `Preloaded Assets`
list.

These settings can be found under `Other Settings`. Doing just one of these
things may be enough to make it work, but it can't hurt to be sure either way.

## License

DokoDemoPainter (code, models and textures) is distributed under the MIT
license. This means that you can use it, modify it, distribute and so on in
whatever way you like, as long as you include the copyright and license
information with your program. It is not necessary to display this information
on a credits screen or similar, as long as it is included at all, but it would
be appreciated.

If you use DokoDemoPainter in your project, it would also make me very happy
to hear about it.

While it is not mandatory, if you make any improvements to DokoDemoPainter,
I would also appreciate it if you contributed them back so I can include them.

The rabbit model, textures and animations included in the example scenes are
distributed under the CC0 license or public domain and were created by Čestmír
Dammer.

## About

DokoDemoPainter was made by [Emiliana](https://twitter.com/emiliana_vt) for
Virtual YouTuber purposes, but it can also be used for games or other
applications.

# Happy painting!