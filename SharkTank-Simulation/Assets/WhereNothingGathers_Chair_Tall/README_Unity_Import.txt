ORGANIZATION XIII THRONE ("WHERE NOTHING GATHERS") — UNITY IMPORT GUIDE
=======================================================================

1. INCLUDED FILES
   - WhereNothingGathers_Chair_Tall.obj : 3D mesh (1 Unity unit = 1 meter)
   - WhereNothingGathers_Chair_Tall.mtl : Material definitions (auto-imported by Unity)

2. MODEL DIMENSIONS & PIVOT
   - Total Height : 19.72 meters (~65 feet tall)
   - Pedestal Base: 0.0m to 18.0m (monumental stepped column + top footrest platform)
   - Chair / Seat : 18.0m to 19.72m (standard human-scale throne: 0.42m seat height, 0.88m width)
   - Pivot Point  : (0, 0, 0) at the exact bottom center of the floor plinth.
   - Forward Axis : +Z (matches Unity's standard Forward vector).

3. HOW TO IMPORT INTO UNITY
   a) Drag BOTH the .obj and .mtl files into your Unity Project's Assets folder.
   b) Select the imported .obj asset in the Project window:
      - In the Model tab: keep Scale Factor = 1.
      - Optional: check "Generate Colliders" if characters will walk/stand on the footrest.
      - In the Materials tab: click "Extract Materials..." if you want to customize the
        gloss/smoothness of OrgXIII_White, OrgXIII_Pedestal, and OrgXIII_SeatTrim.
   c) Drag the model into your Scene and set Y = 0 to place it flush on the floor.

4. CREATING ALL 13 CHAIR HEIGHTS (LIKE IN KINGDOM HEARTS II)
   - In "Where Nothing Gathers", each member's throne sits at a different height
     (with Xemnas highest and Roxas/Vexen lowest).
   - If you want shorter or taller variations without squishing the chair seat:
     either sink the lower pedestal slightly below the floor plane (e.g., Y = -6.0)
     or edit the bottom vertices in Unity ProBuilder / Blender while keeping the
     top throne section (Y >= 18.0m) unscaled.
