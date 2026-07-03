# NavMeshInCompany Redux
Enables the use of AI pathfinding and navigation in the Company Building allowing the spawning of Enemies.

This is an updated version of NavMeshInCompany rebuilt from the ground up.

Here are some changes in this one compared to the previous:
- The NavMesh has been brought up to date with the most recent version of LC. Zeekerss changed it to add a ramp for the company cruiser, but NavMeshInCompany was never updated for it.
- There are more OffMeshLinks than there were before. This means NavMeshAgents can access more places than before.
- Unlike NavMeshInCompany, the NavMesh will only be added at the Company Building Scene! This fixes issues the old mod had where it would replace the NavMesh for ANY moon that had a Company Desk!

> [!NOTE]
> There are plans to get the NavMesh to automatically regen when you land at the company so that mod add buildings, "for example, Lethal Casino," will have a NavMesh added to it.
> Sadly, due to the way the Company Building is setup, automatic generation is BROKEN and some manual changes will be needed. Until then, a static NavMesh will be used.

> [!TIP]
> This mod for some magical reason also fixes Masked Enemies in The Company, therefore the Mask item will actually work.
