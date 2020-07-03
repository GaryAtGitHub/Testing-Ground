using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using Obi;

public class Rope_StickyEnd : MonoBehaviour
{
    public VRTK_InteractableObject m_Handle;
    public Color m_ColorActive = Color.green;
    public Color m_ColorInactive = Color.red;

    private ObiRope obiRope;
    private ObiPinConstraints pinConstrain;
    private ObiPinConstraintBatch pinConstraintBatch;
    private ObiSolver obiSolver;
    private int particleIndexOne;
    private int particleIndexTwo;
    private int contraintIndexOne;
    private int contraintIndexTwo;
    private float particlesDistance;
    private bool isStuck;
    private bool isActivateStick;
    private bool isReleasing;
    private Vector3 particleOffsetOne;
    private Vector3 particleOffsetTwo;

    private void Start()
    {
        obiRope = transform.parent.GetComponentInChildren<ObiRope>();
        pinConstrain = transform.parent.GetComponentInChildren<ObiPinConstraints>();
        pinConstraintBatch = pinConstrain.GetFirstBatch();
        obiSolver = transform.parent.GetComponentInChildren<ObiSolver>();
        int LastParticleConstrainIndex = pinConstraintBatch.GetConstraintsInvolvingParticle(obiRope.UsedParticles - 1).Count != 0 ?
                                                        pinConstraintBatch.GetConstraintsInvolvingParticle(obiRope.UsedParticles - 1)[0] : -1;
        int FirstParticleConstrainIndex = pinConstraintBatch.GetConstraintsInvolvingParticle(0).Count != 0 ?
                                                        pinConstraintBatch.GetConstraintsInvolvingParticle(0)[0] : -1;
        if (gameObject == pinConstraintBatch.pinBodies[LastParticleConstrainIndex].gameObject)
        {
            particleIndexOne = obiRope.UsedParticles - 1;
            particleIndexTwo = particleIndexOne - 1;
        }
        else if (gameObject == pinConstraintBatch.pinBodies[FirstParticleConstrainIndex].gameObject)
        {
            particleIndexOne = 0;
            particleIndexTwo = 1;
        }
        else
        {
            particleIndexOne = -1;
            particleIndexTwo = -1;
        }

        if (m_Handle != null)
        {
            m_Handle.InteractableObjectUsed += M_Handle_InteractableObjectUsed;
            m_Handle.InteractableObjectUnused += M_Handle_InteractableObjectUnused;
        }
        UpdateIndex();
        particleOffsetOne = pinConstraintBatch.pinOffsets[contraintIndexOne];
        particleOffsetTwo = pinConstraintBatch.pinOffsets[contraintIndexTwo];
        Debug.Log(particleOffsetOne);
        Debug.Log(particleOffsetTwo);
        GetComponent<MeshRenderer>().material.color = m_ColorInactive;
    }

    private void OnDestroy()
    {
        if (m_Handle != null)
        {
            m_Handle.InteractableObjectUsed -= M_Handle_InteractableObjectUsed;
            m_Handle.InteractableObjectUnused -= M_Handle_InteractableObjectUnused;
        }
    }

    private void M_Handle_InteractableObjectUsed(object sender, InteractableObjectEventArgs e)
    {
        if (!isStuck && !isReleasing)
        {
            ActivateSticky();
        }
        else
        {
            if (!isReleasing)
            {
                isReleasing = true;
                ReleaseStuckObject();
            }
        }
    }

    private void M_Handle_InteractableObjectUnused(object sender, InteractableObjectEventArgs e)
    {
        if (!isStuck)
        {
            DisActivateSticky();
        }
        if (isReleasing)
        {
            isReleasing = !isReleasing;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isActivateStick) return;
        if (collision.transform.CompareTag("StickcableObject"))
        {
            ObiCollider ObjectToStick = collision.gameObject.GetComponent<ObiCollider>();
            Vector3 Normal = collision.GetContact(0).normal;
            pinConstrain.RemoveFromSolver(null);
            Vector3 DistanceVector = transform.TransformPoint(particleOffsetOne) -
                                               transform.TransformPoint(particleOffsetTwo);
            particlesDistance = DistanceVector.magnitude;
            SetPinConstraints(ObjectToStick, Normal);
            pinConstrain.AddToSolver(null);
            isStuck = true;
            DisActivateSticky();
        }
    }

    private void SetPinConstraints(ObiCollider objectToStick, Vector3 normal)
    {
        pinConstraintBatch.RemoveConstraint(contraintIndexOne);
        UpdateIndex();
        pinConstraintBatch.RemoveConstraint(contraintIndexTwo);
        UpdateIndex();
        gameObject.SetActive(false);
        pinConstraintBatch.AddConstraint(particleIndexOne, objectToStick,
            objectToStick.transform.InverseTransformPoint(obiRope.GetParticlePosition(particleIndexOne) - transform.lossyScale.x * normal), Quaternion.identity, 0);

        //UpdateIndex();

        pinConstraintBatch.AddConstraint(particleIndexTwo, objectToStick,
            objectToStick.transform.InverseTransformPoint(obiRope.GetParticlePosition(particleIndexOne) - transform.lossyScale.x * normal + particlesDistance * normal), Quaternion.identity, 0);
        UpdateIndex();
        //obiCollider.ParentChange();                
    }

    private void UpdateIndex()
    {
        contraintIndexOne = pinConstraintBatch.GetConstraintsInvolvingParticle(particleIndexOne).Count != 0 ?
                                                  pinConstraintBatch.GetConstraintsInvolvingParticle(particleIndexOne)[0] : -1;

        contraintIndexTwo = pinConstraintBatch.GetConstraintsInvolvingParticle(particleIndexTwo).Count != 0 ?
                                                  pinConstraintBatch.GetConstraintsInvolvingParticle(particleIndexTwo)[0] : -1;
    }

    private void ActivateSticky()
    {
        if (isActivateStick) return;
        GetComponent<MeshRenderer>().material.color = m_ColorActive;
        isActivateStick = true;
    }

    private void DisActivateSticky()
    {
        if (!isActivateStick) return;
        GetComponent<MeshRenderer>().material.color = m_ColorInactive;
        isActivateStick = false;
    }

    private void ReleaseStuckObject()
    {
        Vector3 DistanceVector = obiRope.GetParticlePosition(particleIndexOne) - obiRope.GetParticlePosition(particleIndexTwo);
        pinConstrain.RemoveFromSolver(null);
        pinConstraintBatch.RemoveConstraint(contraintIndexOne);
        UpdateIndex();
        pinConstraintBatch.RemoveConstraint(contraintIndexTwo);
        UpdateIndex();
        gameObject.SetActive(true);
        GetComponent<Collider>().isTrigger = true;
        transform.position = obiRope.GetParticlePosition(particleIndexOne) + DistanceVector.normalized * particleOffsetOne.magnitude * transform.lossyScale.x;

        pinConstraintBatch.AddConstraint(particleIndexOne, GetComponent<ObiCollider>(),
             transform.InverseTransformPoint(obiRope.GetParticlePosition(particleIndexOne)), Quaternion.identity, 0);
        pinConstraintBatch.AddConstraint(particleIndexTwo, GetComponent<ObiCollider>(),
              transform.InverseTransformPoint(obiRope.GetParticlePosition(particleIndexOne)).normalized * particleOffsetTwo.magnitude, Quaternion.identity, 0);
        UpdateIndex();
        pinConstrain.AddToSolver(null);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StickcableObject"))
        {
            gameObject.GetComponent<Collider>().isTrigger = false;
            isStuck = false;
        }
    }
}
