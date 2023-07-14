using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;

public partial struct NetworkedPrefabsComponent : IComponentData
{
    public FixedList512Bytes<int> hashCodes;

    public FixedList512Bytes<Entity> prefabs;

    public Entity GetEntity(int hashCode)
    {
        for (int i = 0; i < hashCodes.Length; i++)
        {
            if (hashCodes[i] == hashCode) return prefabs[i];
        }

        throw new System.Exception($"unable to find entity with hashcode: {hashCode}");
    }

    public bool TryGetEntity(int hashCode, out Entity entity)
    {
        for (int i = 0; i < hashCodes.Length; i++)
        {
            if (hashCodes[i] == hashCode)
            {
                entity = prefabs[i];
                return true;
            }
        }

        entity = Entity.Null;
        return false;
    }

    public void AddPrefab(int hashCode, Entity entity)
    {
        hashCodes.Add(hashCode);
        prefabs.Add(entity);
    }
}