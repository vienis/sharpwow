﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWoW.Models.MDX
{
    public class M2Manager
    {
        internal M2Manager()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="df"></param>
        public uint AddInstance(string modelName, ADT.Wotlk.MDDF df)
        {
            renderLock.WaitOne();
            int hash = modelName.ToLower().GetHashCode();
            float wowRotY = Utils.SharpMath.mirrorAngle(df.orientationX);
            float wowRotZ = df.orientationY;
            float wowRotX = df.orientationZ;
            wowRotY *= 0.017453f;
            wowRotZ *= 0.017453f;
            wowRotX *= 0.017453f;
            wowRotZ += (float)Math.PI / 2.0f;
            wowRotZ = Utils.SharpMath.mirrorAngleRadian(wowRotZ);

            if (BatchRenderers.ContainsKey(hash))
            {
                uint id = BatchRenderers[hash].AddInstance(-Utils.Metrics.MidPoint + df.posX, -Utils.Metrics.MidPoint + df.posZ, df.posY, df.scaleFactor / 1024.0f, new SlimDX.Vector3(wowRotY, wowRotX, wowRotZ));
                renderLock.ReleaseMutex();
                return id;
            }
            try
            {
                M2BatchRender rndr = new M2BatchRender(modelName);
                uint ret = rndr.AddInstance(-Utils.Metrics.MidPoint + df.posX, -Utils.Metrics.MidPoint + df.posZ, df.posY, df.scaleFactor / 1024.0f, new SlimDX.Vector3(wowRotY, wowRotX, wowRotZ));
                BatchRenderers.Add(hash, rndr);
                renderLock.ReleaseMutex();
                return ret;
            }
            catch (Exception)
            {
                renderLock.ReleaseMutex();
                throw;
            }
        }

        public void RemoveInstance(string name, uint instanceId)
        {
            renderLock.WaitOne();

            int hash = name.ToLower().GetHashCode();
            if (BatchRenderers.ContainsKey(hash))
            {
                var rendr = BatchRenderers[hash];
                rendr.RemoveInstance(instanceId);

                if (rendr.NumInstances == 0)
                {
                    Game.GameManager.M2ModelCache.ReleaseInfo(rendr.ModelName);
                    rendr.Unload();
                }
            }

            renderLock.ReleaseMutex();
        }

        private System.Threading.Mutex renderLock = new System.Threading.Mutex();
        private Dictionary<int, M2BatchRender> BatchRenderers = new Dictionary<int, M2BatchRender>();
    }
}
