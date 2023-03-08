using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

[Serializable]
public class SimpleAnchorBinding : IStorageBinding
{
    /// <summary>
    /// Storage field used for locally persisting TransformBindings across device boot ups.
    /// </summary>
    public static BindingsLocalStorage<SimpleAnchorBinding> Storage = new BindingsLocalStorage<SimpleAnchorBinding>("transformbindings.json");

    public string Id
    {
        get { return this.id; }
    }

    public MLAnchors.Anchor Anchor
    {
        get { return this.anchor; }
    }

    public string JsonData;

    [SerializeField, HideInInspector]
    private string id;

    [SerializeField, HideInInspector]
    private MLAnchors.Anchor anchor;

    public bool Bind(MLAnchors.Anchor anchor, string jsonData)
    {
        this.JsonData = jsonData;
        id = anchor.Id;
        this.anchor = anchor;

        Storage.SaveBinding(this);

        return true;
    }

    public bool UnBind()
    {
        return Storage.RemoveBinding(this);
    }

}
