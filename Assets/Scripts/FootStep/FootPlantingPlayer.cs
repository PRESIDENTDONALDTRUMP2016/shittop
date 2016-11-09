using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public abstract class FootPlantingPlayer : MonoBehaviour
{
    // The different surfaces and their sounds.
    public AudioSurface defaultSurface;
    public List<AudioSurface> customSurfaces;

    public void PlayFootFallSound(FootStepObject footStepObject)
    {
        for (int i = 0; i < customSurfaces.Count; i++)
            if (customSurfaces[i] != null && ContainsTexture(footStepObject.name, customSurfaces[i]))
            {
                customSurfaces[i].PlayRandomClip(footStepObject);
                return;
            }
        if (defaultSurface != null)
            defaultSurface.PlayRandomClip(footStepObject);
    }

    // check if AudioSurface Contains texture in TextureName List
    private bool ContainsTexture(string name, AudioSurface surface)
    {
        foreach (string _name in surface.TextureOrMaterialNames)
            if (name.Contains(_name))
                return true;

        return false;
    }
}
