objects = \
	compatibility.o \
	dywapitchtrack/dywapitchtrack.o \
	dywapitchtrack/ptDyWa.o \
	FFT/FFT.o \
	FFT/RealFFTf.o \
	Helper.o \
	performous/pitch.o \
	ptAKF.o \
	PitchWrapper.o

CXX = g++
CXXFLAGS = -std=gnu++11 -fPIC -O2

libPitchTracker.so: $(objects)
	$(CXX) -shared -o libPitchTracker.so -fPIC $(objects)
	strip libPitchTracker.so

clean:
	rm -f libPitchTracker.so $(objects)
