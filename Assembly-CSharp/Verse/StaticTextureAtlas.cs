using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Verse;

public class StaticTextureAtlas
{
	public readonly TextureAtlasGroupKey groupKey;

	private List<Texture2D> textures = new List<Texture2D>();

	private Dictionary<Texture2D, Texture2D> masks = new Dictionary<Texture2D, Texture2D>();

	private Dictionary<Texture, StaticTextureAtlasTile> tiles = new Dictionary<Texture, StaticTextureAtlasTile>();

	private Texture2D colorTexture;

	private Texture2D maskTexture;

	public const int MaxTextureSizeForTiles = 512;

	public const int TexturePadding = 8;

	public Texture2D ColorTexture => colorTexture;

	public Texture2D MaskTexture => maskTexture;

	public static int MaxPixelsPerAtlas => MaxAtlasSize / 2 * (MaxAtlasSize / 2);

	public static int MaxAtlasSize => SystemInfo.maxTextureSize;

	public StaticTextureAtlas(TextureAtlasGroupKey groupKey)
	{
		this.groupKey = groupKey;
		colorTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
	}

	public void Insert(Texture2D texture, Texture2D mask = null)
	{
		if (groupKey.hasMask && mask == null)
		{
			Log.Error("Tried to insert a mask-less texture into a static atlas which does have a mask atlas");
		}
		if (!groupKey.hasMask && mask != null)
		{
			Log.Error("Tried to insert a mask texture into a static atlas which does not have a mask atlas");
		}
		textures.Add(texture);
		if (mask != null && groupKey.hasMask)
		{
			masks.Add(texture, mask);
		}
	}

	public void Bake(bool rebake = false)
	{
		if (rebake)
		{
			foreach (KeyValuePair<Texture, StaticTextureAtlasTile> tile in tiles)
			{
				Object.Destroy(tile.Value.mesh);
			}
			tiles.Clear();
		}
		List<Texture2D> destroyTextures = new List<Texture2D>();
		try
		{
			Texture2D[] array = textures.Select(delegate(Texture2D t)
			{
				if (!t.isReadable)
				{
					Texture2D texture2D = TextureAtlasHelper.MakeReadableTextureInstance(t);
					destroyTextures.Add(texture2D);
					return texture2D;
				}
				return t;
			}).ToArray();
			DeepProfiler.Start("Texture2D.PackTextures()");
			Rect[] array2 = colorTexture.PackTextures(array, 8, MaxAtlasSize, makeNoLongerReadable: false);
			DeepProfiler.End();
			colorTexture.name = "TextureAtlas_" + groupKey.ToString() + "_" + colorTexture.GetInstanceID();
			if (groupKey.hasMask)
			{
				maskTexture = new Texture2D(colorTexture.width, colorTexture.height, TextureFormat.ARGB32, mipChain: false);
			}
			for (int i = 0; i < array2.Length; i++)
			{
				Texture2D key = textures[i];
				if (masks.TryGetValue(key, out var value))
				{
					Rect rect = array2[i];
					int x = (int)(rect.xMin * (float)colorTexture.width);
					int y = (int)(rect.yMin * (float)colorTexture.height);
					if (!value.isReadable)
					{
						Texture2D texture2D2 = TextureAtlasHelper.MakeReadableTextureInstance(value);
						destroyTextures.Add(texture2D2);
						value = texture2D2;
					}
					DeepProfiler.Start("maskTexture.SetPixels()");
					maskTexture.SetPixels(x, y, textures[i].width, textures[i].height, value.GetPixels(0), 0);
					DeepProfiler.End();
				}
			}
			if (maskTexture != null)
			{
				maskTexture.name = "Mask_" + colorTexture.name;
				DeepProfiler.Start("maskTexture.Apply()");
				maskTexture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
				DeepProfiler.End();
			}
			if (array2.Length != array.Length)
			{
				Log.Error("Texture packing failed! Clearing out atlas...");
				textures.Clear();
				return;
			}
			for (int j = 0; j < array.Length; j++)
			{
				Mesh mesh = TextureAtlasHelper.CreateMeshForUV(array2[j], 0.5f);
				mesh.name = "TextureAtlasMesh_" + groupKey.ToString() + "_" + mesh.GetInstanceID();
				tiles.Add(textures[j], new StaticTextureAtlasTile
				{
					atlas = this,
					mesh = mesh,
					uvRect = array2[j]
				});
			}
			if (Prefs.TextureCompression)
			{
				DeepProfiler.Start("Texture2D.Compress()");
				if (colorTexture != null)
				{
					colorTexture.Compress(highQuality: true);
				}
				if (maskTexture != null)
				{
					maskTexture.Compress(highQuality: true);
				}
				DeepProfiler.End();
			}
			DeepProfiler.Start("Texture2D.Apply()");
			if (colorTexture != null)
			{
				colorTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			}
			if (maskTexture != null)
			{
				maskTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			}
			DeepProfiler.End();
		}
		finally
		{
			foreach (Texture2D item in destroyTextures)
			{
				Object.Destroy(item);
			}
		}
	}

	public bool TryGetTile(Texture texture, out StaticTextureAtlasTile tile)
	{
		return tiles.TryGetValue(texture, out tile);
	}

	public void Destroy()
	{
		Object.Destroy(colorTexture);
		Object.Destroy(maskTexture);
		foreach (KeyValuePair<Texture, StaticTextureAtlasTile> tile in tiles)
		{
			Object.Destroy(tile.Value.mesh);
		}
		textures.Clear();
		tiles.Clear();
	}
}
