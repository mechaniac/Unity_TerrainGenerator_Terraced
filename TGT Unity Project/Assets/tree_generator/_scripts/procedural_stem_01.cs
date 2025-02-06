using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace tree_gen
{
    public class procedural_stem_01 : MonoBehaviour
    {

        public int numberOfBranches;
        public float branchScale = 1f;
        public float branchScaleRandom = .4f;

        public float smoosh = 0f;
        public float randomise = 0f;

        public float randomOffsetMax = 1f;
        public procedural_branch_01[] branchPrefabs;
        public GameObject leavesPrefab;
        public float leafSize = 1f;
        public GameObject subleavesPrefab;

        public SubLeaveArrangement subLeaveArrangement;
        public int subLeaveAmount;
        public float subLeaveSize;
        public float subLeaveRadius = 0.01f;

        public Vector3 subLeaveOffset;
        public Vector3 subLeafOffsetRotation;
        public Vector3 rotations = new(0, 360, 0);

        public Vector3 rotationRandoms = new(0, 1, 0);
        public int seed = 123;
        System.Random r;
        // List<GameObject> branches = new();

        procedural_branch_01[] branches;
        public Transform[] branchParents;

        public Transform[] leaveParents;    //Collected from branches

        [SerializeField]
        public bool showGizmos;

        void Start()
        {

            // InstantiateBranchesAndLeaves();
        }

        public void InstantiateBranchesAndLeaves(int seed)
        {
            DestroyExistingBranches();

            r = new System.Random(seed);
            branches = new procedural_branch_01[numberOfBranches];
            // Vector3[] branchPositions = GenerateRandomPositions(branchParents);


            for (int i = 0; i < numberOfBranches; i++)
            {
                procedural_branch_01 b = Instantiate(branchPrefabs[r.Next(0, branchPrefabs.Length)], transform);
                b.name = "Branch" + i; // Assign a unique name
                float randomFloat = (float)r.NextDouble() * branchScaleRandom; //(maximum - minimum) + minimum;
                float finalBranchScale = branchScale + randomFloat;
                b.transform.localScale = new Vector3(finalBranchScale, finalBranchScale, finalBranchScale);

                branches[i] = b;

                if (leavesPrefab != null)
                {
                    b.GenerateLeavesAndSubleavesFromPrefab(leavesPrefab, leafSize, subleavesPrefab, subLeaveArrangement, subLeaveAmount, subLeaveRadius, subLeaveSize, subLeaveOffset, subLeafOffsetRotation);
                }
            }
            SetBranchesPositionsAlongPathPoints(branchParents, branches);

            Vector3[] branchRotations = GenerateRandomRotations(rotations, numberOfBranches, rotationRandoms);
            for (int i = 0; i < numberOfBranches; i++)
            {
                branches[i].transform.Rotate(branchRotations[i]);
                // branches[i].transform.position = branchPositions[i];

            }
            // leaveParents = CollectLeaveParents("null4");
            // foreach(Transform t in leaveParents){
            //     // Debug.Log(t.position);
            // }
        }

        public void DestroyExistingBranches()
        {
            // Destroy all existing child branches
            if (branches != null)
            {
                foreach (procedural_branch_01 b in branches)
                {
                    if (b != null)
                    {
                        // Debug.Log("b destroyed " + b.name);
                        DestroyImmediate(b.gameObject);
                    }
                }
                branches = null;
            }
            DeleteAllBranches();
        }

        void DeleteAllBranches()
        {
            // Collect all matching game objects
            List<GameObject> branchesToDelete = new List<GameObject>();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                // Check if the child's name starts with "Branch"
                if (child.name.StartsWith("Branch"))
                {
                    branchesToDelete.Add(child.gameObject);
                }
            }

            // Delete the collected game objects
            foreach (GameObject branchToDelete in branchesToDelete)
            {
                DestroyImmediate(branchToDelete);
            }
        }

        Transform[] CollectLeaveParents(string n)
        {
            // Get all
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);

            // Filter by name
            Transform[] r = System.Array.FindAll(allChildren, child => child.name == n);

            return r;
        }

        public void SetBranchesPositionsAlongPathPoints(Transform[] pathPoints, procedural_branch_01[] branches)
        {
            if(pathPoints[0].transform.position == pathPoints[1].transform.position){
                Debug.LogError("branch parents NOT set apart. adjust branchparents in prefab");
                return;
            }
            if (pathPoints.Length < 2 || branches.Length == 0)
            {
                Debug.LogError("Insufficient path points or branches for interpolation.");
                return;
            }

            // Clamp the input values to their valid ranges
            smoosh = Mathf.Clamp(smoosh, -1f, 1f);
            randomise = Mathf.Clamp(randomise, 0f, 1f);

            // Calculate total path length and distances between points
            float[] segmentLengths = new float[pathPoints.Length - 1];
            float totalLength = 0f;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
                totalLength += segmentLengths[i];
            }

            // Adjust total length based on the smoosh value
            float adjustedTotalLength = totalLength * (1f - Mathf.Abs(smoosh));

            // Place each branch along the path
            for (int i = 0; i < branches.Length; i++)
            {
                // Determine the target length along the path for this branch
                float targetLength = (adjustedTotalLength * i) / (branches.Length - 1);
                targetLength += totalLength * smoosh; // Adjust target length by smoosh factor
                targetLength += (float)r.NextDouble() * randomOffsetMax * randomise; // Add random offset

                // Ensure targetLength is within the path length
                targetLength = Mathf.Clamp(targetLength, 0f, totalLength);

                float accumulatedLength = 0f;
                int segmentIndex = 0;

                // Find the segment where the branch should be placed
                while (segmentIndex < segmentLengths.Length && accumulatedLength + segmentLengths[segmentIndex] < targetLength)
                {
                    accumulatedLength += segmentLengths[segmentIndex];
                    segmentIndex++;
                }

                // Calculate the interpolation factor within the segment
                float segmentT = (targetLength - accumulatedLength) / (segmentLengths[Mathf.Min(segmentIndex, segmentLengths.Length - 1)]);

                // Interpolate position and rotation between the current and next path points
                Vector3 interpolatedPosition = Vector3.Lerp(pathPoints[segmentIndex].position, pathPoints[Mathf.Min(segmentIndex + 1, pathPoints.Length - 1)].position, segmentT);
                // Debug.Log(interpolatedPosition);
                Quaternion interpolatedRotation = Quaternion.Slerp(pathPoints[segmentIndex].rotation, pathPoints[Mathf.Min(segmentIndex + 1, pathPoints.Length - 1)].rotation, segmentT);

                // Debug.Log("highestPoint: " + pathPoints[pathPoints.Length - 1].transform.position.y);
                // Debug.Log("branch: " + branches[i].name);
                // Debug.Log("placement: " + interpolatedPosition.y);
                // Debug.Log("uvShift: " + interpolatedPosition.y / pathPoints[pathPoints.Length - 1].transform.position.y);
                MeshFilter meshFilter;
                // Debug.Log("MeshFilter: " + meshFilter.name);

                for (int c = 0; c < branches[i].transform.childCount; c++)
                {
                    Transform child = branches[i].transform.GetChild(c);
                    meshFilter = child.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        // Duplicate the mesh to avoid modifying the original mesh
                        Mesh mesh = Instantiate(meshFilter.sharedMesh);

                        // Get the mesh's UV coordinates
                        Vector2[] uv = mesh.uv;
                        float meshShift = interpolatedPosition.y / pathPoints[pathPoints.Length - 1].transform.position.y;
                        // Shift the UV coordinates up by 0.5
                        for (int u = 0; u < uv.Length; u++)
                        {
                            uv[u] += new Vector2(0f, meshShift / 1.5f);
                        }

                        // Apply the modified UV coordinates to the mesh
                        mesh.uv = uv;

                        // Assign the modified mesh to the mesh filter
                        meshFilter.mesh = mesh;
                    }
                }


                // Set the branch's position and rotation
                
                branches[i].transform.position = interpolatedPosition;
                branches[i].transform.rotation = interpolatedRotation;
            }
        }

        Vector3[] GenerateRandomPositions(Transform[] transforms)
        {
            if (transforms == null || transforms.Length < 2)
            {
                Debug.LogError("Invalid input array. It should have at least two Transforms.");
                return null;
            }

            Vector3[] randomPositions = new Vector3[numberOfBranches];

            for (int i = 0; i < numberOfBranches; i++)
            {
                float t = (float)r.NextDouble();
                int segmentIndex = r.Next(0, transforms.Length - 1);

                Transform startTransform = transforms[segmentIndex];
                Transform endTransform = transforms[segmentIndex + 1];

                // Interpolate position along the line segment
                Vector3 startPosition = startTransform.position;
                Vector3 endPosition = endTransform.position;
                Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
                randomPositions[i] = position;
            }

            return randomPositions;
        }

        Vector3[] GenerateRandomRotations(Vector3 rotMax, int count, Vector3 randomAmount)
        {
            Vector3[] rotations = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                rotations[i].x = Mathf.Lerp(rotMax.x, rotMax.x * (float)r.NextDouble(), randomAmount.x);
                rotations[i].y = Mathf.Lerp(rotMax.y, rotMax.y * (float)r.NextDouble(), randomAmount.y);
                rotations[i].z = Mathf.Lerp(rotMax.z, rotMax.z * (float)r.NextDouble(), randomAmount.z);
            }

            return rotations;
        }
        public enum SubLeaveArrangement
        {
            Circular,
            Octahedron,
            Tetrahedron
        }

    }
}