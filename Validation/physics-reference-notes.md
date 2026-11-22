# Motion reference provenance

The fixture `physics-reference.csv` is produced by the SMB1 movement subroutine in
SMB-Vanilla commit `20f4e02614412f9dbd407dd4e4fab4ade5eb79da`, compiled with MSVC
14.44 in C11 mode and signed-overflow wrapping enabled. It uses the original SMB1
speed/friction/jump tables as represented by that port. No NES ROM is executed.

The reference harness sets initial X to 40, Y to the standing height, Mario small,
ground area, no directional collision blocking, and feeds NES controller masks.
It decrements the run timer, calls the movement subroutine, updates moving
direction and previous buttons, and applies a shared flat-floor landing rule.
Both implementations expose subpixel positions rather than only the drawn pixel.
Signed world X is used for the leftward case. The executable and source harness
are retained under `_zcode_tmp/archive/development-2026-10-15`.
This directory is an archived development snapshot. To regenerate the fixture,
copy its contents back to the project root's `_zcode_tmp` directory before using
the saved build command; the harness paths retain their original working layout.
The Unity validator consumes the saved CSV directly and does not need the archive.

The 12 cases are each 180 frames:

1. Walk right.
2. Run right.
3. Run 90 frames, then release all buttons.
4. Run right 90 frames, then reverse left while holding B.
5. Tap A for one frame.
6. Hold A for 90 frames.
7. Run, jump at frame 90, hold A through frame 134.
8. Run, jump, reverse direction in the air, then release direction.
9. Run left.
10. Run 90 frames, release B while continuing right.
11. Press right, B and A together from rest.
12. Walk, tap jump, then repress and hold A while already airborne.

`MarioValidation.ReferenceMotionChecks` compares the native C# controller with
the saved fixture and fails on any nonzero position difference. This is a real
cross-implementation regression fixture; it is not generated from ClassicMotor.
It does not certify swimming, terrain collision, controller-to-display latency,
enemy behavior, or cycle-accurate NES hardware timing.
