﻿using UnityEngine;
namespace RollercoasterEdit
{
	public class Main : IMod
    {
        public string Identifier { get; set; }
		public static AssetBundleManager AssetBundleManager = null;
        public static Configuration Configeration = null;


        public void onEnabled()
        {
            if (Main.Configeration == null) {
                Configeration = new Configuration ();
                Configeration.Load (Path);
                Configeration.Save (Path);

            }

			if (Main.AssetBundleManager == null) {

				AssetBundleManager = new AssetBundleManager (this);
			}
			ScriptableSingleton<UIAssetManager>.Instance.trackBuilderWindowGO.gameObject.AddComponent <TrackUIHandle>();

		}

        public void onDisabled()
        {
			UnityEngine.Object.Destroy (ScriptableSingleton<UIAssetManager>.Instance.trackBuilderWindowGO.gameObject.GetComponent<TrackUIHandle> ());
        }

        public string Name
        {
            get { return "Track Edit"; }
        }

        public string Description
        {
            get { return "Allows the User to modify track Path"; }
        }


		public string Path { get; set; }

    }
}
