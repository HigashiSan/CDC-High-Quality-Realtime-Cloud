How to render realistic volume cloud is a topic I always want to try. So far, I have implemented most of the basic cloud rendering technologies, and the effect looks good. About layered cloud and other more advanced technologies, I will implement them slowly after I finish my graduation project.


References:

[Siggraph15_Schneider_Real-Time_Volumetric_Cloudscapes_of_Horizon_Zero_Dawn.pdf](https://github.com/HigashiSan/CDC-High-Quality-Realtime-Cloud/files/11010200/Siggraph15_Schneider_Real-Time_Volumetric_Cloudscapes_of_Horizon_Zero_Dawn.pdf)

[A Ray-Box Intersection Algorithm and Efficient Dynamic Voxel Rendering.pdf](https://github.com/HigashiSan/CDC-High-Quality-Realtime-Cloud/files/11010201/A.Ray-Box.Intersection.Algorithm.and.Efficient.Dynamic.Voxel.Rendering.pdf)


[Wrenninge-OzTheGreatAndVolumetric.pdf](https://github.com/HigashiSan/CDC-High-Quality-Realtime-Cloud/files/11010204/Wrenninge-OzTheGreatAndVolumetric.pdf)


![image](https://user-images.githubusercontent.com/56297955/226152860-e45af740-3d6f-430b-9802-eafecc0dc606.png)

![Screenshot 2023-03-06 105332](https://user-images.githubusercontent.com/56297955/223182922-5df4aa63-b863-44e9-9a5e-b85b3b13c5c3.png)


https://user-images.githubusercontent.com/56297955/223186168-4bb132ff-4699-4f0f-9278-794150bf25c8.mp4


https://user-images.githubusercontent.com/56297955/223188066-da025b37-ff2d-41ca-8118-65041681e4ef.mp4


## Key technical points:

GPU 3D Worley-Perlin noise and FBM Worley detail noise generator use compute shader.

HG phase function.

Multiple scattering approximate.


