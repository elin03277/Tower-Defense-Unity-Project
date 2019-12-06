using UnityEngine;
using System.Collections;
using System;

public class Turret : MonoBehaviour {

	private Transform target;
	private Enemy targetEnemy;

	[Header("General")]

	public float range = 15f;

	[Header("Use Bullets (default)")]
	public GameObject bulletPrefab;
	public float fireRate = 1f;
	private float fireCountdown = 0f;

	[Header("Use Laser")]
	public bool useLaser = false;

	public int damageOverTime = 30;
	public float slowAmount = .5f;

	public LineRenderer lineRenderer;
	public ParticleSystem impactEffect;
	public Light impactLight;

	[Header("Unity Setup Fields")]

	public string enemyTag = "Enemy";

	public Transform partToRotate;
	public float turnSpeed = 10f;

	public Transform firePoint;

    float shortestDistance = Mathf.Infinity;
    GameObject nearestEnemy = null;

    State state;
    
    // Holds the list of states for the machine
    public enum State
    {
        Idle,
        TargetLocked,
        Shoot
    }

    // Model of the tower
    public class Tower
    {
        public bool EnemyAppeared { get; set; }
        public float EnemyInRange { get; set; }
        public float CooldownOver { get; set; }
    }

    public abstract class Decision
    {
        public abstract void Evaluate(Tower tower, ref State state);
    }

    // Questions to test if it's positive or negative
    public class DecisionQuery : Decision
    {
        public Decision Positive { get; set; }
        public Decision Negative { get; set; }
        public Func<Tower, bool> Test { get; set; }

        public override void Evaluate(Tower tower, ref State state)
        {
            bool result = this.Test(tower);

            if (result) this.Positive.Evaluate(tower, ref state);
            else this.Negative.Evaluate(tower, ref state);
        }
    }

    // Sets the state based on the result from the main decision tree
    public class DecisionResult : Decision
    {
        public State Result { get; set; }
        public override void Evaluate(Tower tower, ref State state)
        {
            state = Result;
        }
    }

    // Decision tree logic
    private static DecisionQuery MainDecisionTree()
    {
        var cooldownBranch = new DecisionQuery
        {
            Test = (tower) => tower.CooldownOver <= 0f,
            Positive = new DecisionResult { Result = State.Shoot },
            Negative = new DecisionResult { Result = State.TargetLocked }
        };

        var inRangeBranch = new DecisionQuery
        {
            Test = (tower) => tower.EnemyInRange <= 15f,
            Positive = cooldownBranch,
            Negative = new DecisionResult { Result = State.TargetLocked }
        };

        var trunk = new DecisionQuery
        {
            Test = (tower) => tower.EnemyAppeared,
            Positive = inRangeBranch,
            Negative = new DecisionResult { Result = State.Idle }
        };

        return trunk;
    }

    // Update is called once per frame
    void Update () {

        var trunk = MainDecisionTree();

        var tower = new Tower
        {
            EnemyAppeared = EnemyAppeared(),
            EnemyInRange = shortestDistance,
            CooldownOver = fireCountdown
        };

        trunk.Evaluate(tower, ref state);

        switch(state)
        {
            case State.Idle:
                Idle();
                target = null;
                break;
            case State.TargetLocked:
                LockOnTarget();
                fireCountdown -= Time.deltaTime;
                break;
            case State.Shoot:
                Shoot();
                fireCountdown = 1f / fireRate;
                break;
        }
        /*
        if (state == 0)
        {
            Idle();
            target = null;
        }
        else if(state == 1)
        {
            LockOnTarget();
            fireCountdown -= Time.deltaTime;
        }
        else if(state == 2)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
        */
	}

    bool EnemyAppeared()
    {

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        shortestDistance = Mathf.Infinity;
        nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;

                return true;
            }
        }

        return false;
    }

	void LockOnTarget ()
	{
        target = nearestEnemy.transform;
        targetEnemy = nearestEnemy.GetComponent<Enemy>();
        Vector3 dir = target.position - transform.position;
		Quaternion lookRotation = Quaternion.LookRotation(dir);
		Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
		partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
	}

	void Shoot ()
	{
		GameObject bulletGO = (GameObject)Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
		Bullet bullet = bulletGO.GetComponent<Bullet>();

		if (bullet != null)
			bullet.Move(target);
	}

    public void Idle ()
    {
        Quaternion lookRotation = Quaternion.LookRotation(Vector3.zero);
        Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
        partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
    }

	void OnDrawGizmosSelected ()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, range);
	}
}