# URP Blurred Buffer Outlines
## A render feature to add outlines to your project utilizing vertex colors 

![](https://github.com/Anthelmed/Unity-URP-Blurred-Buffer-Outlines/blob/main/Preview.gif)

**Unity version**: 2022.2 and above

**Package URL**: ``` https://github.com/Anthelmed/Unity-URP-Blurred-Buffer-Outlines.git ```

**How to**: to make it work you have to add vertex colors to your model by yourself (either in your 3D software or directly inside Unity), be aware that the final color is going to be inverted because by default a model that doesn't have any vertex color attribute is fully white in Unity.

**Known issues**: if the outlines are invisible try changing the camera AA mode and/or toggling the Stop NaNs option.
