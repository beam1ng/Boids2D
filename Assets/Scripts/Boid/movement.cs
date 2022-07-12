using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class movement : MonoBehaviour
{
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float fov = 90;
    [SerializeField] private float visionRange = 3;
    [SerializeField] private int raysCount = 10;
    [SerializeField] private float cohesionAmplifier = 0.1f;
    [SerializeField] private float avoidanceAmplifier = 0.1f;
    [SerializeField] private float alignmentAmplifier = 0.1f;
    [SerializeField] private float terrainAvoidanceAmplifier = 1;

    enum MyEnum
    {
        
    }
    private Vector3 _rawCorrectionFromCohesion;
    private Vector3 _rawCorrectionFromActorAvoidance;
    private Vector3 _rawCorrectionFromAlignment;
    private Vector3 _rawCorrectionFromTerrainAvoidance;
    private Vector3 _correctionFromCohesion;
    private Vector3 _correctionFromActorAvoidance;
    private Vector3 _correctionFromAlignment;
    private Vector3 _correctionFromTerrainAvoidance;
    private Vector3 _finalCorrection;
    private readonly List<Vector3> _offsetsToSeenActors = new List<Vector3>();
    private RayScan _middleRay;
    private RayScan _bestRay;
    private float _speedModifier = 1;
    private RayScan[] _rayScans;
    private struct RayScan
    {
        public Ray Ray { get; set; }
        public RaycastHit Hit { get; set; }
    }

    void Update()
    {
        ProcessMovement();
    }

    private void ProcessMovement()
    {
        AnalyzeSurroundings();
        CalculateCorrections();
        MoveAlongCorrection(_finalCorrection);
    }

    private void AnalyzeSurroundings()
    {
        var rayScans = CastRays().ToList();
        var centerOfCloseBoids = transform.position;
        var directionOfCloseBoids = transform.forward;
        var boids = GameObject.FindGameObjectsWithTag(tag);
        var closeBoidsCount = 1;
        
        _bestRay = SelectBestRay(rayScans);
        _middleRay = SelectMiddleRay(rayScans);
        _rawCorrectionFromActorAvoidance = Vector3.zero;
        _offsetsToSeenActors.Clear();

        foreach (var boid in boids)
        {
            if (boid == gameObject) continue;
            
            var distance = Vector3.Distance(transform.position, boid.transform.position);
            if (distance > visionRange || distance < 0.05) continue;
            
            var angularOffsetToBoid = Mathf.Abs(Vector3.Angle(
                boid.transform.position - transform.position,
                transform.forward));
            if (angularOffsetToBoid > fov / 2) continue;

            closeBoidsCount++;
            
            centerOfCloseBoids += boid.transform.position;
            directionOfCloseBoids += boid.transform.forward;

            var awayOffsetFromCloseBoid = (transform.position - boid.transform.position);
            var xAwayRawCorrection = Mathf.Pow(1 / awayOffsetFromCloseBoid.magnitude, 2) * awayOffsetFromCloseBoid.x;
            var yAwayRawCorrection = Mathf.Pow(1 / awayOffsetFromCloseBoid.magnitude, 2) * awayOffsetFromCloseBoid.y;
            _rawCorrectionFromActorAvoidance += new Vector3(xAwayRawCorrection, yAwayRawCorrection, 0);

            _offsetsToSeenActors.Add(-1 * awayOffsetFromCloseBoid);
        }
        centerOfCloseBoids /= closeBoidsCount;
        directionOfCloseBoids /= closeBoidsCount;
        
        _rawCorrectionFromAlignment = directionOfCloseBoids;
        _rawCorrectionFromCohesion = (centerOfCloseBoids - transform.position);
        _rawCorrectionFromTerrainAvoidance = _bestRay.Ray.direction * ((Mathf.Pow((visionRange - _bestRay.Hit.distance) / visionRange, 2) / 2 + 0.5f));
        _finalCorrection = _correctionFromTerrainAvoidance + _correctionFromCohesion + _correctionFromActorAvoidance + _correctionFromAlignment;
        
        _speedModifier = _middleRay.Hit.collider is null? 1 : 1 - (visionRange - _middleRay.Hit.distance) / visionRange;
    }

    private void CalculateCorrections()
    {
        _correctionFromCohesion = Vector3.ClampMagnitude(_rawCorrectionFromCohesion * cohesionAmplifier, 1);
        _correctionFromAlignment = Vector3.ClampMagnitude(_rawCorrectionFromAlignment * alignmentAmplifier, 1);
        _correctionFromActorAvoidance = Vector3.ClampMagnitude(_rawCorrectionFromActorAvoidance * avoidanceAmplifier, 2);
        _correctionFromTerrainAvoidance = _rawCorrectionFromTerrainAvoidance * terrainAvoidanceAmplifier;
    }

    private IEnumerable<RayScan> CastRays()
    {
        if (raysCount <= 1) return null;
        var raysArray = new Ray[raysCount];
        _rayScans = new RayScan[raysCount];
        var direction = transform.forward;
        for (var rayIndex = 0; rayIndex < raysCount; rayIndex++)
        {
            var angle = -fov / 2 + (float)rayIndex / (raysCount - 1) * fov;
            var preciseDirection = Quaternion.AngleAxis(angle, new Vector3(0, 0, 1)) * direction;
            var ray = new Ray(transform.position, preciseDirection * visionRange);
            RaycastHit hit;
            Physics.Raycast(transform.position, ray.direction, out hit, visionRange,LayerMask.GetMask($"Borders"));

            var rayScan = new RayScan();
            rayScan.Ray = ray;
            rayScan.Hit = hit;
            _rayScans[rayIndex] = rayScan;
        }

        return _rayScans;
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
            //this iterates indexes beginning in the middle and alternating between left and right
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

    private RayScan SelectMiddleRay(IEnumerable<RayScan> rayScans)
    {
        return rayScans.ToList().ElementAt(rayScans.Count() / 2);
    }

    private void MoveAlongCorrection(Vector3 correction)
    {
        if (correction.magnitude < 0.05) return;
        var targetLookVector = Vector3.RotateTowards(transform.forward, correction, 2 * Mathf.PI * Time.deltaTime * 0.75f, 0.1f);
        transform.rotation = Quaternion.LookRotation(new Vector3(targetLookVector.x,targetLookVector.y,0));

        var positionOffset = _speedModifier * speed * Time.deltaTime;
        var targetPosition = transform.forward * positionOffset;
        transform.position += new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var position = transform.position;
        Gizmos.DrawSphere(position + _correctionFromCohesion,0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(position + _correctionFromActorAvoidance,0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(position+_correctionFromAlignment,0.05f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(position + _correctionFromTerrainAvoidance, 0.05f);
        Gizmos.color = Color.black;
        foreach (var offset in _offsetsToSeenActors)
        {
            Gizmos.DrawRay(transform.position,offset);
        }

        foreach (var rayScan in _rayScans)
        {
            if (rayScan.Hit.collider is null)
            {
                Debug.DrawRay(rayScan.Ray.origin,rayScan.Ray.direction*visionRange,Color.green);
            }
            else
            {
                Debug.DrawRay(rayScan.Ray.origin,rayScan.Ray.direction*visionRange,Color.red);
            }
        }
    }
}
