WHERE NOTHING GATHERS — CENTER PLATFORM (NO CENTRAL SYMBOL)
===========================================================

1. INCLUDED FILES
   - WhereNothingGathers_CenterPlatform.obj : 3D mesh (1 Unity unit = 1 meter)
   - WhereNothingGathers_CenterPlatform.mtl : Material definitions (auto-imported by Unity)

2. DIMENSIONS & COORDINATES
   - Outer Diameter : 21.2 meters (Radius = 10.6m; 22.2m across the 13 perimeter buttresses)
   - Arena Floor Top: Y = +0.75 meters (clean circular center, NO Nobody/Org XIII symbol)
   - Outer Step Base: Y =  0.00 meters (3 stepped risers from Y = 0.00m up to Y = 0.75m)
   - Under-Structure: Y = -0.35 meters down to Y = -7.50 meters (finished floating underside)
   - Pivot Point    : (0, 0, 0) at the center of the platform on the Y = 0.00m outer ring plane.

3. HOW TO IMPORT & ALIGN WITH YOUR 13 CHAIRS IN UNITY
   a) Drag BOTH .obj and .mtl files into your Unity Project's Assets folder.
   b) Select the imported .obj asset in the Project window:
      - In the Model tab: keep Scale Factor = 1 and check "Generate Colliders" so
        your player character can walk up the 3 steps onto the arena floor.
      - In the Materials tab: click "Extract Materials..." if you want to customize
        Platform_White, Platform_Center, or Platform_Trim.
   c) Place the Center Platform at Position (0, 0, 0).
   d) To arrange the 13 tall chairs around the platform:
      - Place each chair at radius R = 12.2m to 13.0m from (0, 0, 0), rotated to face inward
        toward (0, 0, 0), spaced every 27.69 degrees (360 / 13).
      - The 13 radial floor lines and 13 perimeter buttresses on this platform are spaced at
        exact 27.69-degree intervals so every chair lines up with a radial axis.
