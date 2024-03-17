using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XREquipmentGrab : XRGrabInteractable
{
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        //Equipment equipment = GetComponent<Equipment>();
        //if(attachTransform != null)
        //{
        //    Rigidbody attachedRigidbody = attachTransform.GetComponent<Rigidbody>();
        //    if(equipment.attachedTo != null && attachedRigidbody != null)
        //        attachedRigidbody.isKinematic = true;
        //}
    }
    //float angularDrag;
    //bool useGravity;
    //bool isKinematic;
    //Rigidbody m_rigidbody;
    //protected override void SetupRigidbodyDrop(Rigidbody rigidbody)
    //{
    //    //rigidbody.angularDrag = angularDrag;
    //    //rigidbody.useGravity = useGravity;
    //    //rigidbody.isKinematic = isKinematic;
    //    base.SetupRigidbodyDrop(rigidbody);
    //    //Equipment equipment = GetComponent<Equipment>();
    //    ////Rigidbody m_rigidbody = GetComponent<Rigidbody>();
    //    //if (equipment.attachedTo != null)
    //    //{
    //    //    rigidbody.useGravity = false;
    //    //    rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    //    //}
    //    //else
    //    //{
    //    //    rigidbody.useGravity = true;
    //    //    rigidbody.constraints = RigidbodyConstraints.None;
    //    //}
    //}

    //protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
    //{
    //    //m_rigidbody = rigidbody;
    //    base.SetupRigidbodyGrab(rigidbody);
    //    //angularDrag = rigidbody.angularDrag;
    //    //useGravity = rigidbody.useGravity;
    //    //isKinematic = rigidbody.isKinematic;
    //    //rigidbody.angularDrag = 0;
    //    //rigidbody.useGravity = false;
    //    //rigidbody.isKinematic = true;
    //}
}
