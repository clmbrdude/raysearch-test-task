# raysearch-test-task
Test task for RaySearch
## How to run
* Load solution in Visual Studio
* Edit the App.config and add location of your app.js file
* Compile
* Run from Visual studio or use the xunit.runner.console
* N.B the node server is started and restarted from within the tests.

## Bugs
* In consultations data; dates change from unspecified timezone in the form YY-MM-DD to
UTC in the form YY-MM-DD:hhmmss.uuuZ hence when truncating to a date the consultation appears to 
be scheduled one day too soon. This results in three failing test cases
* The server crashes in a specific scenario

## Observations
* Rooms are not booked optimally (patient not requiring machines are booked in first available room)
* Doctors are not booked optimally 
* Several requests contains a consultations property that should not be there

## Other test that should be developed
* Add patients from multiple threads and check consistency
* Verify JSON schema for all responses.
* Test load limit (e.g. number of requests per second)
* Maybe add tests that tests random permutations of requests, e.g. model based tests.
* More exploratory testing

## Lession learned
* When developing tests, always read the app state as last step and verify consistency. I missed a problem where 
consultation data for patient1 changed after patient2 was added. Since I was only checking patient1's data immediately after adding 
the patient this was initially missed.
* Sadly enough finding bugs only from automation (e.g. the crash bug) is **really** hard, exploratory or more exhaustive automated testing is needed, 
e.g. model based tests.
* Async methods propagates through all code. 
