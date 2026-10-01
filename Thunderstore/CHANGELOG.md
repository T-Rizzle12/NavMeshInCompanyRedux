# Changelog

## 1.3.0 - 2026-09-8
A minor backend change has been made to use NavMeshLib. 
Custom NavMeshAgent IDs should now be automatically supported as a result.

## 1.2.2 - 2026-07-20
Actually update the Changelog file...........

## 1.2.1 - 2062-07-20
Fixed a rare issue where the company NavMesh would parent itself to the wrong object and fail to clean itself after the ship leaves the company building.

## 1.2.0 - 2026-07-6
We now override NavMeshInCompany when both mods are present

## 1.1.1 - 2026-07-6
- Fixed CSync not being marked as a dependency for the mod

## 1.1.0 - 2026-07-5
- Fixed dynamic NavMesh generation at the company building
- Added some config options to control the dynamic regeneration of the Company Navmesh
- Added more dropdown links to the Company NavMesh
- Fixed some parts of the NavMesh being generated inside of inaccessible areas of the Company Building

## 1.0.0 - 2026-07-3
- Initial release