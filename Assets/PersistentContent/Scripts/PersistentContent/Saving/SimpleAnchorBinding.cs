using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace PersistentContentExample
{
    /// <summary>
    /// Simple Binding script that binds string data to and anchor ID so it can be restored based on the available anchors.
    /// </summary>
    [Serializable]
    public class SimpleAnchorBinding : IStorageBinding
    {
        /// <summary>
        /// Storage field used for locally persisting TransformBindings across device boot ups.
        /// </summary>
        public static BindingsLocalStorage<SimpleAnchorBinding> Storage =
            new BindingsLocalStorage<SimpleAnchorBinding>("SimpleAnchorBindings.json");

        public string Id
        {
            get { return this.id; }
        }

        public MLAnchors.Anchor Anchor
        {
            get { return this.anchor; }
        }

        public string Extras
        {
            get { return extras; }
        }

        public bool IsBound
        {
            get { return isBound; }
        }

        [SerializeField, HideInInspector] private bool isBound;

        [SerializeField, HideInInspector] private string extras;

        [SerializeField, HideInInspector] private string id;

        [SerializeField, HideInInspector] private MLAnchors.Anchor anchor;


        public bool Bind(MLAnchors.Anchor anchor, string extras)
        {
            this.id = anchor.Id;
            this.anchor = anchor;
            this.extras = extras;
            return Storage.SaveBinding(this);
        }

        public bool UnBind()
        {
            return Storage.RemoveBinding(this);
        }
    }
}