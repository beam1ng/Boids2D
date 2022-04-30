using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class movement : MonoBehaviour
{
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float fov = 90;
    [SerializeField] private float visionRadius = 3;
    [SerializeField] private int raysCount = 10;
    [SerializeField] private float cohesionAmplifier = 0.1f;
    [SerializeField] private float avoidanceAmplifier = 0.1f;
    [SerializeField] private float alignmentAmplifier = 0.1f;
    [SerializeField] private float terrainAvoidanceAmplifier = 1;
    private Vector3 _cohesionVector;
    private Vector3 _avoidanceVector;
    private Vector3 _alignmentVector;
    private Vector3 _borderAvoidanceVector;
    private readonly List<Vector3> _vectorsToSeenActors = new();
    private float _speedModifier = 1;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        ProcessMovement();
    }

    void ProcessMovement()
    {
        var raysScans = CastRays();
        var bestRay = SelectBestRay(raysScans);
        var middleRay = SelectMiddleRay(raysScans);
        _borderAvoidanceVector = (Mathf.Pow((visionRadius - bestRay.Hit.distance)/visionRadius, 2)/2+0.5f) * bestRay.Ray.direction * terrainAvoidanceAmplifier;
        _speedModifier = middleRay.Hit.collider is null? 1 : 1 - (visionRadius - middleRay.Hit.distance) / visionRadius;
        
        CalculateMovementOffsets();
        Vector3 finalDirectionVector = _borderAvoidanceVector + _cohesionVector + _avoidanceVector + _alignmentVector;
        MoveAlongDirection(finalDirectionVector);

    }

    private RayScan SelectMiddleRay(IEnumerable<RayScan> raysScans)
    {
        return raysScans.ToList().ElementAt(raysScans.Count() / 2);
    }

    private struct RayScan
    {
        public Ray Ray { get; set; }
        public RaycastHit Hit { get; set; }
    }

    private IEnumerable<RayScan> CastRays()
    {
        if (raysCount <= 1) return null;
        var raysArray = new Ray[raysCount];
        var rayScans = new RayScan[raysCount];
        var direction = transform.forward;
        for (var rayIndex = 0; rayIndex < raysCount; rayIndex++)
        {
            var angle = -fov / 2 + (float)rayIndex / (raysCount - 1) * fov;
            var preciseDirection = Quaternion.AngleAxis(angle, new Vector3(0, 0, 1)) * direction;
            var ray = new Ray(transform.position, preciseDirection * visionRadius);
            RaycastHit hit;
            Physics.Raycast(transform.position, ray.direction, out hit, visionRadius,LayerMask.GetMask($"Borders"));

            var rayScan = new RayScan();
            rayScan.Ray = ray;
            rayScan.Hit = hit;
            rayScans[rayIndex] = rayScan;

            if (hit.collider is null)
            {
                // Debug.DrawRay(ray.origin,ray.direction*visionRadius,Color.green);

            }
            else
            {
                // Debug.DrawRay(ray.origin,ray.direction*visionRadius,Color.red);

            }
        }

        return rayScans;
    }

    private RayScan SelectBestRay(IEnumerable<RayScan> rayScans)
    {
        var rayScansList = rayScans.ToList();
        var n = rayScansList.Count();
        var maximumHitDistance = 0.0f;
        var maximumHitDistanceRayIndex = 0;
        
        for (var i = 0; i < n; i++)
        {
            var rayScanIndex =
                (int)n / 2 + (2 * (i % 2) - 1) * (i + 1) / 2;
            //this iterates indexes beginning in the middle and spreading left and right
            var currentRayScan = rayScansList.ElementAt(rayScanIndex);
            if (currentRayScan.Hit.collider is null)
            {
                var previousAdjectiveIndex = i != 0 ? Mathf.Max(i - 2, 0) : 1;
                var nextAdjectiveIndex = i != n - 1 ? Mathf.Min(i + 2, n - 1) : n-2;
                var previousAdjectiveRayScan =
                    rayScansList.ElementAt(n / 2 + (2 * (previousAdjectiveIndex % 2) - 1) *
                        (previousAdjectiveIndex + 1) / 2);
                var nextAdjectiveRayScan = rayScansList.ElementAt(n / 2 + (2 * (nextAdjectiveIndex % 2) - 1) *
                    (nextAdjectiveIndex + 1) / 2);
                if(previousAdjectiveRayScan.Hit.collider is null && nextAdjectiveRayScan.Hit.collider is null)
                    return currentRayScan;
            };
            if (!(currentRayScan.Hit.distance > maximumHitDistance)) continue;
            maximumHitDistance = currentRayScan.Hit.distance;
            maximumHitDistanceRayIndex = rayScanIndex;
        }

        return rayScansList.ElementAt(maximumHitDistanceRayIndex);
    }

    private void CalculateMovementOffsets()
    {
        var centerOfCloseBoids = transform.position;
        var directionOfCloseBoids = transform.forward;
        var closeBoids = 1;

        var boids = GameObject.FindGameObjectsWithTag(tag);
        var boidsAvoidanceVector = Vector3.zero;

        _vectorsToSeenActors.Clear();
        foreach (var boid in boids)
        {
            if (boid == gameObject) continue;
            var dist = Vector3.Distance(transform.position, boid.transform.position);
            if (dist > visionRadius || dist < 0.05) continue;
            if (!(Mathf.Abs(Quaternion.Angle(Quaternion.LookRotation(boid.transform.position - transform.position),
                    transform.rotation)) < fov / 2)) continue;

            closeBoids++;
            var position = boid.transform.position;
            centerOfCloseBoids += position;
            directionOfCloseBoids += boid.transform.forward;

            var awayVector = (transform.position - position);
            var xForce = Mathf.Pow(1 / awayVector.magnitude, 3) * awayVector.x;
            var yForce = Mathf.Pow(1 / awayVector.magnitude, 3) * awayVector.y;
            boidsAvoidanceVector += new Vector3(xForce, yForce, 0);

            _vectorsToSeenActors.Add(-1 * awayVector);
        }

        if (closeBoids != 1)
        {
            centerOfCloseBoids /= closeBoids;
            directionOfCloseBoids /= closeBoids;
        }

        _cohesionVector =
            Vector3.ClampMagnitude((centerOfCloseBoids - transform.position) * cohesionAmplifier, 1);
        _alignmentVector = Vector3.ClampMagnitude(directionOfCloseBoids * alignmentAmplifier, 1);
        _avoidanceVector = Vector3.ClampMagnitude(boidsAvoidanceVector * avoidanceAmplifier, 2);
    }

    private void MoveAlongDirection(Vector3 dir)
    {
        if (dir.magnitude < 0.05) return;
        var targetLookVector = Vector3.RotateTowards(transform.forward, dir, 2 * Mathf.PI * Time.deltaTime * 0.2f, 0.1f);
        transform.rotation = Quaternion.LookRotation(new Vector3(targetLookVector.x,targetLookVector.y,0));

        var posOffset = _speedModifier * speed * Time.deltaTime;
        var targetPos = transform.forward * posOffset;
        transform.position += new Vector3(targetPos.x, targetPos.y, transform.position.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var position = transform.position;
        Gizmos.DrawSphere(position + _cohesionVector,0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(position + _avoidanceVector,0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(position+_alignmentVector,0.05f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(position + _borderAvoidanceVector, 0.05f);
        Gizmos.color = Color.black;
        foreach (var vec in _vectorsToSeenActors)
        {
            Gizmos.DrawRay(transform.position,vec);
        }
    }
}
