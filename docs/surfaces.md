# Surfaces

When a player is on one of the surfaces below, their character moves differently.

Each surface is depicted with a specific texture to indicate it's type.

This is the core mechanic of the game. Navigating the different surfaces to reach the goal / end of each level.

## Ground

On ground, movement is crisp and snappy. The unit moves at a fixed speed in a straight line toward the click point, stops exactly on it, and can instantly change direction mid-move if a new point is clicked. No momentum, no deceleration.

## Slidy

The unit moves at a fixed constant speed at all times. It cannot stop. Clicking a destination sets the target heading — the unit rotates toward it at a fixed turning rate (degrees per second), regardless of how far away the click is. If the unit reaches the clicked point it continues in whatever direction it was heading. Clicking a stationary point causes the unit to orbit it in a circle. Clicking behind the unit causes it to arc around and reverse course.

## Fast

Identical to Slidy but everything is 2x — speed and turning rate both doubled.

## Confusing / Backwards

Identical to Slidy except the target direction is inverted — the unit steers away from the click point instead of toward it. Speed and turning rate are the same as Slidy.

## Fast Confusing / Backwards

Identical to Confusing but everything is 2x — speed and turning rate both doubled.

## Straight

The unit continues in whatever direction it was heading when it entered this surface. Right-clicking does nothing — the player cannot redirect at all. Abilities still work normally. The unit regains control the moment it exits onto another surface type.

## Kill / Death

If a player touches this surface type they will die and leave behind their icon that others can ressurect. "Don't touch the lava" 