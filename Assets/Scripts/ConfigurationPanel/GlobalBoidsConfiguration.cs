using UnityEngine;
using UnityEngine.UI;

public class GlobalBoidsConfiguration : MonoBehaviour
{
    private float _cohesionAmplifier;
    private float _alignmentAmplifier;
    private float _avoidanceAmplifier;

    private struct workingPreset
    {
        public const float cohesionAmplifier = 0.8f;
        public const float alignmentAmplifier = 1.0f;
        public const float avoidanceAmplifier = 0.15f;
    }
    
    void Start()
    {
        OnSliderChange();
    }

    public void OnSliderChange()
    {
        ReadSliderValues();
        UpdateBoids();
    }

    public void OnButtonPressed()
    {
        UpdateSliderValues();
        OnSliderChange();
    }

    private void UpdateSliderValues()
    {
        GameObject.FindWithTag("CohesionSlider").GetComponent<Slider>().value = workingPreset.cohesionAmplifier;
        GameObject.FindWithTag("AlignmentSlider").GetComponent<Slider>().value = workingPreset.alignmentAmplifier;
        GameObject.FindWithTag("AvoidanceSlider").GetComponent<Slider>().value = workingPreset.avoidanceAmplifier;
    }

    private void ReadSliderValues()
    {
        _cohesionAmplifier = GameObject.FindWithTag("CohesionSlider").GetComponent<Slider>().value;
        _alignmentAmplifier = GameObject.FindWithTag("AlignmentSlider").GetComponent<Slider>().value;
        _avoidanceAmplifier = GameObject.FindWithTag("AvoidanceSlider").GetComponent<Slider>().value;
    }
    
    private void UpdateBoids()
    {
        var boids = GameObject.FindGameObjectsWithTag("Boid");
        foreach (var boid in boids)
        {
            boid.GetComponent<movement>().UpdateAmplifiers(
                _cohesionAmplifier,
                _alignmentAmplifier,
                _avoidanceAmplifier);
        }
    }
}
