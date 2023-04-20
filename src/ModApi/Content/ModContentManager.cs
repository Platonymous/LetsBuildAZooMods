using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ModLoader.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModLoader.Content
{
    internal class ModContentManager : ContentManager
    {
        private Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        public ModContentManager(IServiceProvider i_service, string i_sRoot) : base(i_service, i_sRoot)
        {
        }


        public override T Load<T>(string i_assetName)
        {
            if (TextureCache.TryGetValue(i_assetName, out Texture2D texture))
                return (T) (object) texture;

            T loaded = InternalLoad<T>(i_assetName);

            if (loaded is Texture2D loadedTexture)
                TextureCache.Add(i_assetName, loadedTexture);

            return loaded;
        }

        public string NormalizeAssetName(string i_assetName)
        {
            i_assetName = i_assetName.Replace(@"/", @"\");
            i_assetName = Path.Combine(i_assetName.Split('\\'));
            return i_assetName;
        }

        public T InternalLoad<T>(string i_assetName)
        {
            i_assetName = NormalizeAssetName(i_assetName);

            ModApi.ApiHelper.Console.Log("Loading: " + Path.Combine(RootDirectory, i_assetName));

            if (typeof(T) == typeof(Texture2D))
                if (Path.Combine(RootDirectory, i_assetName ) is String file && File.Exists(file))
                    using (var fileStream = new FileStream(file, FileMode.Open))
                        return (T)(object)PremultiplyTransparency(Texture2D.FromStream(ModApi.Game1.GraphicsDevice, fileStream));
                else
                    return (T)(object)null;

            T loaded = base.Load<T>(i_assetName);

            return loaded;
        } 
private Texture2D PremultiplyTransparency(Texture2D texture)
        {
            int count = texture.Width * texture.Height;
            Color[] data = new Color[count];
            try
            {
                texture.GetData(data, 0, count);

                bool changed = false;
                for (int i = 0; i < count; i++)
                {
                    ref Color pixel = ref data[i];
                    if (pixel.A == byte.MinValue || pixel.A == byte.MaxValue)
                        continue;

                    data[i] = new Color(pixel.R * pixel.A / byte.MaxValue, pixel.G * pixel.A / byte.MaxValue, pixel.B * pixel.A / byte.MaxValue, pixel.A);
                    changed = true;
                }

                if (changed)
                    texture.SetData(data, 0, count);
            }
            catch
            {

            }

            return texture;
        }
    }
}
