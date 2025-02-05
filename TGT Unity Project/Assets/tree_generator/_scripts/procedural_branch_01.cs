using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace tree_gen
{
    public class procedural_branch_01 : MonoBehaviour
    {
        public Transform[] leaveParents;



        public void GenerateLeavesAndSubleavesFromPrefab(GameObject l, float lSize, GameObject s_l, procedural_stem_01.SubLeaveArrangement sA, int sAAmount, float saOffset, float saScale, Vector3 offset, Vector3 offsetRotation)
        {
            if (leaveParents != null && l != null)
            {
                foreach (Transform p in leaveParents)
                {
                    // Instantiate the prefab at the leaveParent's position and rotation
                    GameObject o = Instantiate(l, p.position, p.rotation);

                    // Parent the instantiated prefab to the leaveParent
                    o.transform.parent = p;
                    o.transform.localScale = new Vector3(lSize, lSize, lSize);

                    if (s_l != null)
                    {
                        AddSubLeaves(p, s_l, sA, sAAmount, saOffset, saScale, offset, offsetRotation);
                    }

                }
            }
        }

        void AddSubLeaves(Transform parent, GameObject s_l, procedural_stem_01.SubLeaveArrangement sA, int saAmount, float saRadius, float saScale, Vector3 offset, Vector3 offsetRotation)
        {
            Vector3[] subLeavesPoints = new Vector3[0];

            switch (sA)
            {
                case procedural_stem_01.SubLeaveArrangement.Octahedron:
                    subLeavesPoints = GenerateOctahedronPositions(saRadius);
                    break;

                case procedural_stem_01.SubLeaveArrangement.Tetrahedron:
                    subLeavesPoints = GenerateTetrahedronVertices(saRadius);
                    break;

                case procedural_stem_01.SubLeaveArrangement.Circular:
                    subLeavesPoints = GenerateCirclePositions(saAmount, saRadius);
                    // Debug.Log("circle done");
                    break;

                default:
                    break;

            }

            Vector3[] shiftedSubLeavesPoints = ShiftVectorArray(subLeavesPoints, offset);
            subLeavesPoints = RotateVectorArray(shiftedSubLeavesPoints, offsetRotation);


            for (int i = 0; i < subLeavesPoints.Length; i++)
            {
                GameObject o = Instantiate(s_l, Vector3.zero, Quaternion.identity);
                o.transform.parent = parent;
                o.transform.localScale = new Vector3(saScale, saScale, saScale);
                o.transform.localPosition = subLeavesPoints[i];
                o.transform.LookAt(parent);
            }
        }
        Vector3[] GenerateOctahedronPositions()
        {

            Vector3[] octahedronVertices = new Vector3[]
            {
             Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
            };
            return octahedronVertices;
        }

        Vector3[] GenerateOctahedronPositions(float offsetFromCenter)
        {
            Vector3[] octahedronVertices = new Vector3[]
            {
        Vector3.up + Vector3.up * offsetFromCenter,
        Vector3.down + Vector3.down * offsetFromCenter,
        Vector3.left + Vector3.left * offsetFromCenter,
        Vector3.right + Vector3.right * offsetFromCenter,
        Vector3.forward + Vector3.forward * offsetFromCenter,
        Vector3.back + Vector3.back * offsetFromCenter
            };

            return octahedronVertices;
        }
        Vector3[] GenerateCirclePositions(int numberOfPoints, float radius, float xxx)
        {
            Vector3[] circlePositions = new Vector3[numberOfPoints];

            for (int i = 0; i < numberOfPoints; i++)
            {
                float theta = 2 * Mathf.PI * i / numberOfPoints;
                float x = 0f;  // Set x to 0 to have all points aligned in the center
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);

                circlePositions[i] = new Vector3(x, y, z);

            }

            return circlePositions;
        }

        Vector3[] GenerateCirclePositions(int pointCount, float radius)
        {
            Vector3[] positions = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float angle = 360f / pointCount * i;

                float x = radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = radius * Mathf.Sin(Mathf.Deg2Rad * angle);

                positions[i] = new Vector3(x, 0f, z);
            }

            return positions;
        }

        Vector3[] GenerateTetrahedronVertices(float radius)
        {
            float rootTwoThirds = Mathf.Sqrt(2f / 3f);
            float rootOneThird = Mathf.Sqrt(1f / 3f);

            Vector3[] vertices = new Vector3[4];

            // Vertex 0 (top)
            vertices[0] = new Vector3(0f, radius * rootTwoThirds, 0f);

            // Vertex 1 (bottom-front)
            vertices[1] = new Vector3(-radius * rootOneThird, -radius / 3f, radius * rootTwoThirds);

            // Vertex 2 (bottom-left)
            vertices[2] = new Vector3(radius * rootOneThird, -radius / 3f, radius * rootTwoThirds);

            // Vertex 3 (bottom-right)
            vertices[3] = new Vector3(0f, -radius / 3f, -radius * rootTwoThirds);

            return vertices;
        }

        Vector3[] ShiftVectorArray(Vector3[] originalArray, Vector3 shift)
        {
            Vector3[] shiftedArray = new Vector3[originalArray.Length];

            for (int i = 0; i < originalArray.Length; i++)
            {

                shiftedArray[i] = originalArray[i] + shift;
                // Debug.Log("original: ");
                // Debug.Log(originalArray[i]);
                // Debug.Log("shifted: ");
                // Debug.Log(shiftedArray[i]);
            }

            return shiftedArray;
        }

        Vector3[] RotateVectorArray(Vector3[] originalArray, Vector3 rotationAxis, float rotationAngle)
        {
            Quaternion rotationQuaternion = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            Vector3[] rotatedArray = new Vector3[originalArray.Length];

            for (int i = 0; i < originalArray.Length; i++)
            {
                rotatedArray[i] = rotationQuaternion * originalArray[i];
            }

            return rotatedArray;
        }

        Vector3[] RotateVectorArray(Vector3[] originalArray, Vector3 shiftRotation)
        {
            Quaternion rotationQuaternion = Quaternion.Euler(shiftRotation);
            Vector3[] rotatedArray = new Vector3[originalArray.Length];

            for (int i = 0; i < originalArray.Length; i++)
            {
                rotatedArray[i] = rotationQuaternion * originalArray[i];
            }

            return rotatedArray;
        }

        void DrawGizmoSpheres(Vector3[] positions)
        {
            if (positions == null || positions.Length <= 0) return;
            Gizmos.color = Color.yellow;

            foreach (Vector3 position in positions)
            {
                Gizmos.DrawSphere(position, 0.5f);
            }
        }


    }
}