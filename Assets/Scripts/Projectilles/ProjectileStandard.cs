using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileStandard : MonoBehaviour
{
    #region Variables

    #region General
    [Header("General")]
    [Tooltip("Radius of this projectile's collision detection")]
    public float radius = 0.01f;

    [Tooltip("Transform representing the root of the projectile (used for accurate collision detection)")]
    public Transform root;

    [Tooltip("Transform representing the tip of the projectile (used for accurate collision detection)")]
    public Transform tip;

    [Tooltip("LifeTime of the projectile")]
    public float maxLifeTime = 5f;

    [Tooltip("VFX prefab to spawn upon impact")]
    public GameObject impactVFX;
    [Tooltip("LifeTime of the VFX before being destroyed")]
    public float impactVFXLifetime = 5f;
    [Tooltip("Offset along the hit normal where the VFX will be spawned")]
    public float impactVFXSpawnOffset = 0.1f;

    [Tooltip("Distance over which the projectile will correct its course to fit the intended trajectory (used to drift projectiles towards center of screen in First Person view). At values under 0, there is no correction")]
    public float trajectoryCorrectionDistance = -1;
    [Tooltip("Determines if the projectile inherits the velocity that the weapon's muzzle had when firing")]
    public bool inheritWeaponVelocity = false;
    [Tooltip("Downward acceleration from gravity")]
    public float gravityDownAcceleration = 0f;


    #endregion
    public LayerMask hittableLayers;
    ProjectileBase m_ProjectileBase;
    bool m_HasTrajectoryOverride;
    float m_ShootTime;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    List<Collider> m_IgnoredColliders;
    Vector3 m_TrajectoryCorrectionVector;
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    #endregion

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        // hittableLayers = m_ProjectileBase.hittableLayers;
        // hittableLayers = m_ProjectileBase.owner.GetComponent<MyWeaponManager>().canBeShot;
        m_ProjectileBase.onShoot += OnShoot;
        // Debug.Log(LayerMask.LayerToName(hittableLayers.value));
        Destroy(gameObject, maxLifeTime);

    }

    private void OnShoot()
    {
        m_ShootTime = Time.deltaTime;
        m_LastRootPosition = root.position;
        m_Velocity = transform.forward * m_ProjectileBase.speed;
        m_IgnoredColliders = new List<Collider>();
        transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;

        // ignore owner's colliders
        Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);

        // handle case of player shooting
        MyWeaponManager weaponManager = m_ProjectileBase.owner.GetComponent<MyWeaponManager>();
        if (weaponManager)
        {
            m_HasTrajectoryOverride = true;
            Vector3 cameraToMuzzle = (m_ProjectileBase.initialPosition - weaponManager.transform.Find("Cameras/Main Camera").transform.position);

            m_TrajectoryCorrectionVector = Vector3.ProjectOnPlane(-cameraToMuzzle, weaponManager.transform.Find("Cameras/Main Camera").transform.forward);
            if (trajectoryCorrectionDistance == 0)
            {
                transform.position += m_TrajectoryCorrectionVector;
                m_ConsumedTrajectoryCorrectionVector = m_TrajectoryCorrectionVector;
            }
            else if (trajectoryCorrectionDistance < 0)
            {
                m_HasTrajectoryOverride = false;
            }

            if (Physics.Raycast(weaponManager.transform.Find("Cameras/Main Camera").transform.position, cameraToMuzzle.normalized, out RaycastHit hit, cameraToMuzzle.magnitude, hittableLayers, QueryTriggerInteraction.Collide))
            {
                if (IsHitValid(hit))
                {
                    OnHit(hit.point, hit.normal, hit.collider);
                }

            }
        }


    }


    // Update is called once per frame
    void Update()
    {
        // Move
        transform.position += m_Velocity * Time.deltaTime;
        if (inheritWeaponVelocity)
        {
            transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;
        }

        // Drift towards trajectory override (this is so that projectiles can be centered 
        // with the camera center even though the actual weapon is offset)
        if (m_HasTrajectoryOverride && m_ConsumedTrajectoryCorrectionVector.sqrMagnitude < m_TrajectoryCorrectionVector.sqrMagnitude)
        {
            Vector3 correctionLeft = m_TrajectoryCorrectionVector - m_ConsumedTrajectoryCorrectionVector;
            float distanceThisFrame = (root.position - m_LastRootPosition).magnitude;
            Vector3 correctionThisFrame = (distanceThisFrame / trajectoryCorrectionDistance) * m_TrajectoryCorrectionVector;
            correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, correctionLeft.magnitude);
            m_ConsumedTrajectoryCorrectionVector += correctionThisFrame;

            // Detect end of correction
            if (m_ConsumedTrajectoryCorrectionVector.sqrMagnitude == m_TrajectoryCorrectionVector.sqrMagnitude)
            {
                m_HasTrajectoryOverride = false;
            }

            transform.position += correctionThisFrame;
        }

        // Orient towards velocity
        transform.forward = m_Velocity.normalized;

        // Gravity
        if (gravityDownAcceleration > 0)
        {
            transform.position = Vector3.down * gravityDownAcceleration * Time.deltaTime;
        }

        // Hit detection
        {
            RaycastHit closesthit = new RaycastHit();
            closesthit.distance = Mathf.Infinity;
            bool foundHit = false;

            // Sphere cast
            Vector3 displacementSinceLastFrame = tip.position - m_LastRootPosition;
            RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, radius, displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, hittableLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (IsHitValid(hit) && hit.distance < closesthit.distance)
                {
                    foundHit = true;
                    closesthit = hit;
                }
            }

            if (foundHit)
            {
                // Handle case of casting when inside a collider
                if (closesthit.distance <= 0f)
                {
                    closesthit.point = root.position;
                    closesthit.normal = -transform.forward;
                }

                OnHit(closesthit.point, closesthit.normal, closesthit.collider);
            }
        }

        m_LastRootPosition = root.position;
    }


    bool IsHitValid(RaycastHit hit)
    {
        // ignore hits with specific ignored colliders
        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
        {
            return false;
        }
        return true;
    }
    void OnHit(Vector3 point, Vector3 normal, Collider collider)
    {
        // TODO: add damage

        // impact vfx
        if (impactVFX)
        {
            GameObject impactVFXInstance = Instantiate(impactVFX, point + (normal * impactVFXSpawnOffset), Quaternion.LookRotation(normal));
            if (impactVFXLifetime > 0)
            {
                Destroy(impactVFXInstance.gameObject, impactVFXLifetime);
            }
        }

        // TODO: impact sfx

        // self destruct on impact
        Destroy(this.gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, radius);
    }
}
