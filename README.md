# NavMeshInCompany Redux
Enables the use of AI pathfinding and navigation in the Company Building allowing the spawning of Enemies.

You can download the mod here: https://thunderstore.io/c/lethal-company/p/TRizzle/NavMeshInCompanyRedux/

This is an updated version of NavMeshInCompany rebuilt from the ground up.

Here are some changes in this one compared to the previous:
- The NavMesh has been brought up to date with the most recent version of LC. Zeekerss changed it to add a ramp for the company cruiser, but NavMeshInCompany was never updated for it.
- There are more OffMeshLinks than there were before. This means NavMeshAgents can access more places than before.
- Unlike NavMeshInCompany, the NavMesh will only be added at the Company Building Scene! This fixes issues the old mod had where it would replace the NavMesh for ANY moon that had a Company Desk!
- The NavMesh can be dynamically regenerated at runtime. This could allow mod added buildings have NavMesh added to them. It depends on how the mod sets up said building.

> [!TIP]
> This mod for some magical reason also fixes Masked Enemies in The Company, therefore the Mask item will actually work.