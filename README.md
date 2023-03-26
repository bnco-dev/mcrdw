# Monte-Carlo Redirected Walking

Paper: [(link)](https://github.com/bnco-dev/mcrdw/files/11032009/monte-carlo-redirected-walking.pdf)

This respository contains an implementation of the algorithm in Unity and the evaluation code used to produce the results.

## Quickstart

Download the project as a zip and open with the Unity editor. The project has been tested with 2020.3 LTS, though other versions may also work.

In the editor, open the Experiment scene. Play the scene. You should now see an experiment running in your Game window.

## Extending with new methods

Adding new redirection methods to the system for comparison is a 2-stage process:

* Write a script that defines the behaviour of the method
* Create a prefab for the method, with the script + any diagnostic info

### Method scripts

Your script should extend `RDWMethod`, and override 3 methods:

* `_OnAttach` is run before each trial. Use for member variable initialisation etc.
* `Discontinuity` is run when the simulation halts temporarily, e.g., on boundary collision. Use to reset smoothing variables etc.
* `Step` is run at each timestep of the simulation. This is where to define redirection behaviour.

A convenience class, `RDWRedirector`, can (optionally) be used to implement the nuts and bolts of redirections. It takes a `RedirectInstruction` as an argument.

The existing methods are in RDW/Scripts/Methods - `RDWSteerToCenterMethod` is a good starting point.

### Method prefabs

Add a new trial rig prefab, and add it as a condition in your Scheduler (Trial/Scheduler in the Experiment scene). Trial rigs must have at least an `RDWTrackSpace` and a component derived from `RDWMethod`.

The current trial rigs are in RDWExperiment/Prefabs. S2C Trial Rig is a minimalist starting point. MCRDW Trial Rig displays diagnostics data.
