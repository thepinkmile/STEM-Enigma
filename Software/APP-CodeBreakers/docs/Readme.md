# Code Breakers: Enigma 101

This application is built as a learning aid as part of STEM activities.
The main aim of the application is to dive into the history of secure communications from World War 2.
In this era, the Germans used a machine called Enigma.
Towards the end of the war, the british team based at Bletchley Park were able to break the Enigma codes and also developed an improved version named TypeX.
Fastforward to the presaent day and we have so much more technology at our fingertips, so we embark on our own journey to see if we can make a better machine.

This application contains:
- A history of the Enigma machine
- Diagrams and Models of how the Enigma machine was constructed
- An Enigma encode/decode simulator
- A playground for you to change various aspects to see how they effect the encoded messages

## History Section
This section should have tabs for Map, Timeline and Index.
Clicking any "entity" in one of the above mentioned tabs should open and focus on a new tab containing the content for that "entity".
Initial thought is to use Entity Framework and use a databasae with migrations (scripted migrations for use from an installer).
The simplest database for basic content is probably going to be SQLite and created local to the application executable.

A History Entity could be one of the following:
- Person/Team Profile
- Event/Date
- Place/Location
Note: any of these entities could contain links to other entities (for references or attributions).
All content should be provided in MD or RTF format with links to images stored locally to the application (possibly a self extracting resource?).
