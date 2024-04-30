using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PluginHub.Editor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PluginHub.Tests
{

    public class RaycastWithoutColliderTest
    {
        [Test]
        public void SimpleTest()
        {
            Vector3 v0 = new Vector3(1, 1, 0);
            Vector3 v1 = new Vector3(1, -1, 0);
            Vector3 v2 = new Vector3(-1, 0, 0);

            Ray ray = new Ray(new Vector3(0, 0, -5), Vector3.forward);
            bool result = RaycastWithoutCollider.RayIntersectsTriangle(ray.origin, ray.direction, v0, v1, v2,
                out Vector3 hitPoint, out Vector3 normal, true);
            Assert.IsTrue(result);

        }

        [Test]
        public void CanIgnoreBackFace()
        {
            Vector3 v0 = new Vector3(1, 1, 0);
            Vector3 v2 = new Vector3(-1, 0, 0);
            Vector3 v1 = new Vector3(1, -1, 0);

            Ray ray = new Ray(new Vector3(0, 0, -5), Vector3.forward);
            bool result = RaycastWithoutCollider.RayIntersectsTriangle(ray.origin, ray.direction, v0, v2, v1,
                out Vector3 hitPoint, out Vector3 normal, true);
            Assert.IsFalse(result);
        }

    }
}
