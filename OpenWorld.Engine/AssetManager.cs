﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWorld.Engine
{
	/// <summary>
	/// Provides methods to load assets.
	/// </summary>
	public sealed partial class AssetManager
	{
		private readonly AssetCache cache = new AssetCache();
		private readonly ICollection<AssetSource> sources = new List<AssetSource>();
		private readonly List<Asset> loadedAssets = new List<Asset>();

		/// <summary>
		/// Loads an asset.
		/// </summary>
		/// <typeparam name="T">Type of the asset.</typeparam>
		/// <param name="name">Name of the asset. Can contain path information.</param>
		/// <returns>Loaded asset.</returns>
		public T Load<T>(string name)
			where T : Asset
		{
			return this.Load<T>(name, false);
		}

		/// <summary>
		/// Loads an asset.
		/// </summary>
		/// <typeparam name="T">Type of the asset.</typeparam>
		/// <param name="name">Name of the asset. Can contain path information.</param>
		/// <param name="loadNew">If true, the asset will be loaded non-cached.</param>
		/// <returns>Loaded asset.</returns>
		public T Load<T>(string name, bool loadNew)
			where T : Asset
		{
			if (name == null)
				throw new ArgumentNullException("name");

			string selectedExtension;

			if (!this.IsCaseSensitive)
				name = name.ToLower(System.Globalization.CultureInfo.InvariantCulture);

			if (!loadNew && this.cache.Contains<T>(name))
				return this.cache.Get<T>(name);

			T asset = Asset.Load<T>(
				new AssetLoadContext(this, name, Path.GetDirectoryName(name) + "/"),
				this.OpenAssetStream<T>(name, out selectedExtension), 
				selectedExtension);
			asset.assetName = name;
			this.loadedAssets.Add(asset);

			if(!loadNew)
				this.cache.Add<T>(name, asset);
			return asset;
		}

		/// <summary>
		/// Loads an asset synchronous.
		/// </summary>
		/// <typeparam name="T">Type of the asset.</typeparam>
		/// <param name="name">Name of the asset. Can contain path information.</param>
		/// <returns>Loaded asset.</returns>
		public T LoadSync<T>(string name)
			where T : Asset
		{
			return this.LoadSync<T>(name, false);
		}

		/// <summary>
		/// Loads an asset synchronous.
		/// </summary>
		/// <typeparam name="T">Type of the asset.</typeparam>
		/// <param name="name">Name of the asset. Can contain path information.</param>
		/// <param name="loadNew">If true, the asset will be loaded non-cached.</param>
		/// <returns>Loaded asset.</returns>
		public T LoadSync<T>(string name, bool loadNew)
			where T : Asset
		{
			var asset = this.Load<T>(name, loadNew);
			while (!asset.IsLoaded) Thread.Sleep(0);
			return asset;
		}

		/// <summary>
		/// Opens a stream for an asset.
		/// </summary>
		/// <typeparam name="T">Type of the asset to be loaded.</typeparam>
		/// <param name="name">Name of the asset. Can contain path information.</param>
		/// <param name="selectedExtension">The extension that the asset stream has.</param>
		/// <returns>Opened stream to load asset from.</returns>
		private Stream OpenAssetStream<T>(string name, out string selectedExtension)
			where T : Asset
		{
			var extensions = GetValidExtensions<T>();
			
			selectedExtension = null;
			AssetSource assetSource = null;
			foreach (var source in this.Sources)
			{
				foreach (var extension in extensions)
				{
					if(!source.Exists(name + extension))
						continue;
					assetSource = source;
					selectedExtension = extension;
					break;
				}
				if (selectedExtension != null)
					break;
			}
			if (selectedExtension == null)
				throw new AssetNotFoundException(name);

			return assetSource.Open(name + selectedExtension);
		}

		private static string[] GetValidExtensions<T>()
			where T : Asset
		{
			HashSet<string> validExtensions = new HashSet<string>();
			Type type = typeof(T);
			object[] attribs;
			do
			{
				attribs = type.GetCustomAttributes(typeof(AssetExtensionAttribute), false);
				type = type.BaseType;
			} while (attribs.Length == 0 && type != null);
			
			foreach (AssetExtensionAttribute attrib in attribs)
			{
				foreach (var extension in attrib.GetExtensions())
					validExtensions.Add(extension);
			}

			return validExtensions.ToArray();
		}

		/// <summary>
		/// Diposes all loaded assets.
		/// </summary>
		public void CleanUp()
		{
			foreach(var asset in this.loadedAssets)
			{
				asset.Unload();
			}
			this.loadedAssets.Clear();
		}

		/// <summary>
		/// Gets or sets a value that indicates wheather the asset
		/// manager uses case sensitive asset names or not.
		/// </summary>
		public bool IsCaseSensitive { get; set; }

		/// <summary>
		/// Gets a collection of asset sources.
		/// </summary>
		public ICollection<AssetSource> Sources
		{
			get { return sources; }
		}
	}
}
