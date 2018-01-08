using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class Simulation : MonoBehaviour {

	//parameters
	public int numShoppers;
	public float stairLength;
	public float height;
	public int window;
	public float speed;

	//location
	public Transform stairVertex1, stairVertex2, stairVertex3, stairVertex4, 
	stairVertex5, stairVertex6, stairVertex7, stairVertex8;
	public GameObject stair;
	public GameObject secondFloor;
	public Transform spawnArea, spawnAreaSecond;
	public Transform store1,store2,store3,store4,store5,store6,store7,store8,store9,store10,store11,store12;

	//prefabs
	public GameObject obstacle;
	public GameObject shopper;
	public GameObject testCube;

	//keeping track for silvers/ prepping for silvers
	private List<GameObject> obstacles;
	private List<Transform> stairVertexes;
	private List<Vector3> obstacleSpawn;
	private List<Vector3> stores;

	private HashSet<Vector4> reservationTable;
	private HashSet<Vector3> walkableArea;
	private List<GameObject> shoppers;
	private Dictionary<GameObject, Stack<Vector4>> paths;
	private Dictionary<GameObject, Vector3> destination;
	private Dictionary<GameObject, int> maxWindow;

	//random
	private float time;
	private int t;
	private int maxWindowValue;

	//test

	private int steps;
	private int failure;
	private int success;
	private float printTime;

	// Use this for initialization
	void Start () {
		
		stairVertexes = new List<Transform> ();
		obstacles = new List<GameObject> ();
		obstacleSpawn = new List<Vector3> ();
		stores = new List<Vector3> ();

		reservationTable = new HashSet<Vector4> ();
		walkableArea = new HashSet<Vector3> ();
		shoppers = new List<GameObject> ();
		paths = new Dictionary<GameObject,Stack<Vector4>> ();
		destination = new Dictionary<GameObject, Vector3> ();
		maxWindow = new Dictionary<GameObject, int> ();

		time = 0f;
		t = 0;
		maxWindowValue = 100;

		steps = 0;
		failure = 0;
		success = 0;
		printTime = 0f;

		GenerateStairs ();
		SpawnObstacles ();
		SpawnShoppers ();
		InitSilvers ();

		foreach (Vector3 area in walkableArea) {
			var test = (GameObject)Instantiate (
				           testCube,
				           area,
				           Quaternion.identity
			           );
		}
//		test = Instantiate( shopper, new Vector3(-15f,1.5f,0f), Quaternion.identity );
	}
	
	// Update is called once per frame
	void Update () {
		
		if (time >= 0.1f) {
			reservationTable.Clear();
			SimulateShoppers ();

			//reset time variables
			t++;
			time = 0f;

			if (t == window) {
				t = 0;
			}
		}

		if (printTime >= 30f) {
			print ("success:" + success + " failure:" + failure + " total:" + (failure + success) + " avg steps:" + (steps-failure*100) / success);
			printTime = 0f;
		}

		time += Time.deltaTime;
		printTime += Time.deltaTime;
	}

	void SimulateShoppers () {
		Vector4 newLocation;
		int rnd,index;
		//update path for all shoppers
		foreach (GameObject go in shoppers) {

			if (maxWindow [go] > maxWindowValue) {
				go.transform.position = destination [go];
				failure++;
			}

			//choose between going to shop or some place in front of a shop
			if (go.transform.position.Equals(destination[go]) || maxWindow[go] > maxWindowValue) {
				rnd = UnityEngine.Random.Range(0,2);
				if (go.transform.position.Equals (destination [go])) {
					success++;
				}
				if (rnd == 0) {
					//go to a random spot in front of shop
					index = UnityEngine.Random.Range(0,obstacleSpawn.Count);
					destination [go] = obstacleSpawn [index];
					maxWindow [go] = 0;
					//call silvers for path
				} else {
					//go inside a shop
					index = UnityEngine.Random.Range(0,stores.Count);
					destination [go] = stores [index];
					maxWindow [go] = 0;
				}
			}

			//call silvers for path
			if (t == 0) {
				paths[go] = Silvers (go.transform.position, destination[go],walkableArea,reservationTable,window);
			}
			//move shoppers
			if (paths [go].Count > 0) {
				newLocation = paths [go].Pop ();
				go.transform.position = new Vector3 (newLocation.x, newLocation.y, newLocation.z);
				maxWindow [go] ++;
				steps ++;
			}
		}
	}

	void GenerateStairs () {

		Vector3 pB, pA;
		GameObject stairs;

		//move the second floor so that the length of the stairs matches the parameter set

		secondFloor.GetComponent<Transform>().position = new Vector3 (secondFloor.GetComponent<Transform>().position.x, secondFloor.GetComponent<Transform>().position.y + height, secondFloor.GetComponent<Transform>().position.z + stairLength);

		//assuming we put matching vertexes adjacent one to the other ex: 1-2 3-4

		stairVertexes.Add (stairVertex1);
		stairVertexes.Add (stairVertex2);
		stairVertexes.Add (stairVertex3);
		stairVertexes.Add (stairVertex4);
		stairVertexes.Add (stairVertex5);
		stairVertexes.Add (stairVertex6);
		stairVertexes.Add (stairVertex7);
		stairVertexes.Add (stairVertex8);

		float x, lengthC, sineC, angleC;
		Vector3 posC,temp;

		for (int i = 0; i < 8; i += 2) {
			pA = stairVertexes [i].position;
			pB = stairVertexes [i + 1].position;

			posC = ((pB - pA) * 0.5F ) + pA;
			lengthC = (pB - pA).magnitude; //C#
			sineC = ( pB.y - pA.y ) / lengthC; 
			angleC = Mathf.Asin( sineC ) * Mathf.Rad2Deg; 
			if (pB.z < pA.z) {angleC = 0 - angleC;} 

			stairs = Instantiate( stair, posC, Quaternion.identity ); 
			stairs.GetComponent<Transform>().localScale = new Vector3(1, 1, lengthC); 

			stairs.GetComponent<Transform>().rotation = Quaternion.Euler(-1f * angleC, 0, 0);

			//add the Vector3 walkable spots from the stairs to our hashset

			x = (pA.x + pB.x) / 2f;

			int p1Z, p2Z;

			if (pA.z < pB.z) {
				p1Z = (int)pA.z;
				p2Z = (int)pB.z;
			} else {
				p1Z = (int)pB.z;
				p2Z = (int)pA.z;
			}

			for (int j = p1Z; j < p2Z; j ++) {
				temp = new Vector3 (x, 1.5f, (float)j);
				walkableArea.Add(temp);
			}
		}
	}

	void SpawnObstacles () {

		//find and cache the spots where one can spawn an obstacles

		//1st floor

		Vector3 center = spawnArea.position;
		float minX, minZ, maxX, maxZ, y;

		minX = center.x - spawnArea.lossyScale.x / 2f;
		maxX = center.x + spawnArea.lossyScale.x / 2f;

		y = center.y + 1.5f;

		minZ = center.z - spawnArea.lossyScale.z / 2f;
		maxZ = center.z + spawnArea.lossyScale.z / 2f;

		for (int i = (int)minX ; i < (int)maxX + 1; i ++) {
			for (int j = (int)minZ ; j < (int)maxZ + 1; j++) {
				obstacleSpawn.Add (new Vector3 (i, y, j));
			}
		}

		//2nd floor

		center = spawnAreaSecond.position;

		minX = center.x - spawnAreaSecond.lossyScale.x / 2f;
		maxX = center.x + spawnAreaSecond.lossyScale.x / 2f;

		y = center.y + 1.5f;

		minZ = center.z - spawnAreaSecond.lossyScale.z / 2f;
		maxZ = center.z + spawnAreaSecond.lossyScale.z / 2f;

		for (int i = (int)minX ; i < (int)maxX + 1; i ++) {
			for (int j = (int)minZ ; j < (int)maxZ + 1; j++) {
				obstacleSpawn.Add (new Vector3 (i, y, j));
			}
		}

		//randomly choose four locations to spawn an obstacle

		int index;

		for (int k = 0; k < 4; k++) {
			index = UnityEngine.Random.Range (0, obstacleSpawn.Count);
			obstacles.Add(Instantiate( obstacle, obstacleSpawn[index], Quaternion.identity ));
			obstacleSpawn.RemoveAt (index);
		}
	}

	public void SpawnShoppers() {
		int index;

		for (int i = 0; i < numShoppers; i++) {
			index = UnityEngine.Random.Range (0, obstacleSpawn.Count);
			shoppers.Add(Instantiate( shopper, obstacleSpawn[index], Quaternion.identity ));
		}

		foreach (GameObject go in shoppers) {
			paths.Add(go,new Stack<Vector4>());
			destination.Add(go,go.transform.position);
			maxWindow.Add (go, 0);
		}
	}

	public void InitSilvers () {
		
		foreach (Vector3 v in obstacleSpawn) {
			walkableArea.Add (v);
		}
		
		HashSet<Vector3> firstFloorStore = new HashSet<Vector3>() ;
		HashSet<Vector3> secondFloorStore = new HashSet<Vector3>() ;
		firstFloorStore.Add (store1.position);
		firstFloorStore.Add (store2.position);
		firstFloorStore.Add (store3.position);
		firstFloorStore.Add (store4.position);
		firstFloorStore.Add (store5.position);
		firstFloorStore.Add (store6.position);
		secondFloorStore.Add (store7.position);
		secondFloorStore.Add (store8.position);
		secondFloorStore.Add (store9.position);
		secondFloorStore.Add (store10.position);
		secondFloorStore.Add (store11.position);
		secondFloorStore.Add (store12.position);

		foreach (Vector3 v in firstFloorStore) {
			walkableArea.Add (new Vector3(v.x , 1.5f, v.z));
			for (int i = -3; i < 0; i++) {
				for (int j = -1; j < 2; j++) {
					stores.Add (new Vector3 (v.x + (float)j, 1.5f, v.z + (float)i));
				}
			}
		}

		foreach (Vector3 w in secondFloorStore) {
					walkableArea.Add (new Vector3(w.x , 1.5f, w.z));
			for (int i = 1; i < 4; i++) {
				for (int j = -1; j < 2; j++) {
					stores.Add (new Vector3 (w.x + (float)j, 1.5f, w.z + (float)i));
				}
			}
		}

		foreach (Vector3 w in stores) {
			walkableArea.Add (w);
		}
	}

	public int AStar(Vector3 start, Vector3 end, HashSet< Vector3> mallMap){
		//Already evaluated nodes
		HashSet<Vector3> closedSet = new HashSet<Vector3> ();

		//Nodes to evaluate
		HashSet<Vector3> openSet = new HashSet<Vector3> ();
		openSet.Add (start);

		//For each node, where it came from
		Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3> ();

		//cost of going from start to node
		Dictionary<Vector3, int> gScore = new Dictionary<Vector3, int> ();
		foreach (Vector3 entry in mallMap) {
			gScore.Add (entry, int.MaxValue);
		}
		gScore [start] = 0;

		//For each node, cost from start to node, then node to goal (heuristic)
		Dictionary<Vector3, int> fScore = new Dictionary<Vector3, int>();
		foreach (Vector3 entry in mallMap) {
			fScore.Add (entry, int.MaxValue);
		}

		fScore[start] = HeurisiticCostEstimate (start, end);

		while (openSet.Count > 0) {
			Vector3 current = LowestFScore (openSet, fScore);
			if (current.Equals (end)) {
				return ReconstructPath (cameFrom, current);
			}
			openSet.Remove (current);
			closedSet.Add (current);

			//For each neighbor of the current node
			for (int i = 0; i < 4; i++) {
				Vector3 neighbor = Neighbor (current,i);
				if (mallMap.Contains (neighbor)) {
					if (!mallMap.Contains(neighbor)) {
						if (closedSet.Contains (neighbor)) {
							continue;
						} else if (!openSet.Contains (neighbor)) {
							openSet.Add (neighbor);
						}

						int tmp_gScore = gScore [current] + 1;
						if (tmp_gScore >= gScore [neighbor]) {
							continue;
						}

						cameFrom [neighbor] = current;
						gScore [neighbor] = tmp_gScore;
						fScore [neighbor] = gScore [neighbor] + HeurisiticCostEstimate (neighbor, end);
					}
				}
			}
		}

		return int.MaxValue;
	}

	public int HeurisiticCostEstimate(Vector3 start, Vector3 destination) {
		return(int)(Vector3.Distance(start, destination));
	}

	public int distanceBetween(Vector3 start, Vector3 destination) {
		
		return (int)(Mathf.Abs (start.x - destination.x) + Mathf.Abs (start.z - destination.z));
	}

	public int ReconstructPath( Dictionary<Vector3, Vector3> cameFrom, Vector3 current) {
		int total = 0;
		Stack<Vector3> totalPath = new Stack<Vector3>();
		totalPath.Push (current);
		while (cameFrom.ContainsKey(current)) {
			current = cameFrom [current];
			totalPath.Push (current);
			total++;
		}
		return total;
	}

	public Vector3 LowestFScore (HashSet<Vector3> openSet, Dictionary<Vector3, int> fScore) {
		Vector3 lowest = fScore.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
		int lowestFSCore = int.MaxValue;

		foreach (Vector3 v in openSet) {
			if (fScore [v] < lowestFSCore) {
				lowest = v;
				lowestFSCore = fScore [v];
			}
		}
		return lowest;
	}

	public Vector3 Neighbor ( Vector3 current, int direction) {
		switch (direction) {
		case 0:
			//going +x
			return new Vector3(current.x + 1f, current.y, current.z);
		case 1:
			//going -x
			return new Vector3(current.x - 1f, current.y, current.z);
		case 2:
			//going +z
			return new Vector3(current.x, current.y, current.z + 1f);
		case 3:
			//going -z
			return new Vector3(current.x, current.y, current.z - 1f);
		default:
			// stay still
			return new Vector3(current.x, current.y, current.z);
		}
	}

	//	function A*(start, goal)
	//	// The set of nodes already evaluated
	public Stack<Vector4> Silvers(Vector3 start, Vector3 destination, HashSet<Vector3> mallMap, HashSet<Vector4> reservationTable, int window) {

		//convert to 4 dimensions to take account for time TODO
		Vector4 timeStart = new Vector4(start.x,start.y,start.z,0f);
		HashSet<Vector4> closedSet = new HashSet<Vector4>();

		int bestScore = int.MaxValue;
		Vector4 bestEnd = timeStart;

		//		// The set of currently discovered nodes that are not evaluated yet.
		//		// Initially, only the start node is known.
		HashSet<Vector4> openSet = new HashSet<Vector4>();
		openSet.Add (timeStart);

		//		// For each node, which node it can most efficiently be reached from.
		//		// If a node can be reached from many nodes, cameFrom will eventually contain the
		//		// most efficient previous step.
		Dictionary<Vector4, Vector4> cameFrom = new Dictionary<Vector4, Vector4>();

		//		// For each node, the cost of getting from the start node to that node.
		//		gScore is the time variable in this case

		Dictionary<Vector4, int> fScore = new Dictionary<Vector4, int> ();
		//		// For the first node, that value is completely heuristic.
		//		fScore[start] := heuristic_cost_estimate(start, goal)
		fScore.Add (timeStart, HeurisiticCostEstimate(start,destination));
		//

		Vector4 current;
		//		while openSet is not empty
		while (openSet.Count > 0) {
			//			current := the node in openSet having the lowest fScore[] value
			current = LowestFScore( openSet, fScore);

			//			if current = goal
			//				return reconstruct_path(cameFrom, current)

			openSet.Remove (current);
			closedSet.Add (current);

			if ((int)current.w == window) {
				Vector3 current3 = new Vector3(current.x, current.y, current.z);
				if (destination.Equals( current3)) {
					return ReconstructPath ( cameFrom , current, reservationTable);
				}
				int totalCost;
				int endCost = AStar ( current3, destination, mallMap);
				if (endCost == int.MaxValue) {
					totalCost = int.MaxValue;
				} else {
					totalCost = (int)current.w + endCost;
				}
				if (totalCost <= bestScore) {
					bestScore = totalCost;
					bestEnd = current;
				}
				continue;
			}

			for (int i = 0; i < 5; i++) {
				Vector4 neighbor = Neighbor (current, i);

				if (reservationTable.Contains (neighbor)) {
					continue;
				}

				Vector3 neighborPosition = new Vector3 (neighbor.x, neighbor.y, neighbor.z);

				if (mallMap.Contains (neighborPosition)) {
					if (closedSet.Contains (neighbor)) {
						continue;
					} else if (!openSet.Contains (neighbor)) {
						openSet.Add (neighbor);
					}

					int tmp_gScore = (int)current.w + 1;
					if (tmp_gScore > (int)neighbor.w) {
						continue;
					}

					cameFrom [neighbor] = current;
					fScore [neighbor] = (int)neighbor.w - HeurisiticCostEstimate (new Vector3(neighbor.x,neighbor.y,neighbor.z), destination);

				}
			}				
		}
		return ReconstructPath(cameFrom, bestEnd, reservationTable);
	}

	public Vector4 LowestFScore (HashSet<Vector4> openSet, Dictionary<Vector4, int> fScore) {
		Vector4 lowest = new Vector4();
		int lowestFSCore = int.MaxValue;

		foreach (Vector4 v in openSet) {
			if (fScore [v] < lowestFSCore) {
				lowest = v;
				lowestFSCore = fScore [v];
			}
		}
		return lowest;
	}

	public Stack<Vector4> ReconstructPath( Dictionary<Vector4, Vector4> cameFrom, Vector4 current, HashSet<Vector4> reservationTable) {
		Stack<Vector4> totalPath = new Stack<Vector4> ();
		totalPath.Push (current);
		Vector4 temp = current;
		while (cameFrom.ContainsKey(temp)) {
			temp = cameFrom[temp];
			totalPath.Push (temp);
			reservationTable.Add (temp);
			reservationTable.Add (new Vector4 (temp.x, temp.y, temp.z, temp.w + 1f));
		}
		return totalPath;
	}

	public Vector4 Neighbor ( Vector4 current, int direction) {
		switch (direction) {
		case 0:
			//staying still t++
			return new Vector4(current.x, current.y, current.z, current.w + 1f);
		case 1:
			//going +x
			return new Vector4(current.x + 1f, current.y, current.z, current.w + 1f);
		case 2:
			//going -x
			return new Vector4(current.x - 1f, current.y, current.z, current.w + 1f);
		case 3:
			//going +z
			return new Vector4(current.x, current.y, current.z + 1f, current.w + 1f);
		case 4:
			//going -z
			return new Vector4(current.x, current.y, current.z - 1f, current.w + 1f);
		default:
			// stay still
			return new Vector4(current.x, current.y, current.z, current.w + 1f);
		}
	}
}